using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.FileService.Interface;
using ESFA.DC.ILR.FundingService.FM36.FundingOutput.Model.Output;
using ESFA.DC.JobContext.Interface;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Extensions.Configuration;
using Spike.FM36Tool.Core;

namespace Spike.FM36Tool.Application.JobContext
{
    public class DcHelper
    {
        private readonly IJsonSerializationService serializationService;
        private readonly ITopicPublishService<JobContextDto> topicPublishingService;
        private readonly IFileService azureFileService;
        private readonly TopicPublishingServiceFactory topicPublishingServiceFactory;

        private readonly string subscriptionName;
        private readonly string storageContainer;
        private readonly string storageConnectionString;

        public DcHelper(IJsonSerializationService serializationService,
            ITopicPublishService<JobContextDto> topicPublishingService,
            IFileService azureFileService, IConfiguration configuration, TopicPublishingServiceFactory topicPublishingServiceFactory)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            this.serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            this.topicPublishingService = topicPublishingService ?? throw new ArgumentNullException(nameof(topicPublishingService));
            this.azureFileService = azureFileService ?? throw new ArgumentNullException(nameof(azureFileService));
            this.topicPublishingServiceFactory = topicPublishingServiceFactory ?? throw new ArgumentNullException(nameof(topicPublishingServiceFactory));
            subscriptionName = configuration.GetSection("DcConfiguration")["SubscriptionName"];
            storageContainer = configuration.GetSection("DcConfiguration")["blobStorageContainer"];
            storageConnectionString = configuration.GetConnectionString("DcStorageConnectionString");
        }

        public async Task SendPeriodEndTask(short academicYear, byte collectionPeriod, long jobId, PeriodEndTask periodEndTask)
        {
            try
            {
                var dto = new JobContextDto
                {
                    JobId = jobId,
                    KeyValuePairs = new Dictionary<string, object>
                    {
                        {JobContextMessageKey.UkPrn, 0 },
                        {JobContextMessageKey.Filename, string.Empty },
                        { JobContextMessageKey.CollectionName, $"PE-DAS-{periodEndTask:G}{academicYear}"},
                        {JobContextMessageKey.CollectionYear, academicYear },
                        { JobContextMessageKey.ReturnPeriod, collectionPeriod },
                        {JobContextMessageKey.Username, "Period End" }
                    },
                    SubmissionDateTimeUtc = DateTime.UtcNow,
                    TopicPointer = 0,
                    Topics = new List<TopicItemDto>
                    {
                        new TopicItemDto
                        {
                            SubscriptionName = "Payments",
                            Tasks = new List<TaskItemDto>
                            {
                                new TaskItemDto
                                {
                                    SupportsParallelExecution = false,
                                    Tasks = new List<string> { periodEndTask.ToString("G") }
                                }
                            }
                        }
                    }
                };
                var publisher = topicPublishingServiceFactory.GetPeriodEndTaskPublisher(periodEndTask);
                await publisher.PublishAsync(dto, new Dictionary<string, object> { { "To", "Payments" } }, "Payments");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task SubmitFM36(List<FM36Learner> learners, long ukprn, short collectionYear,
            byte collectionPeriod, long jobId)
        {
            var ilrSubmission = new FM36Global
            {
                UKPRN = (int)ukprn,
                Year = collectionYear.ToString(),
                Learners = learners
            };
            var json = serializationService.Serialize(ilrSubmission);
            await SubmitFM36(json, ukprn, collectionYear, collectionPeriod, jobId);
        }

        public async Task SubmitFM36(string fm36Json, long ukprn, short academicYear, byte collectionPeriod, long jobId)
        {
            try
            {
                var container = $"ilr{academicYear}-files";
                var messagePointer = $"{ukprn}/{jobId}/FundingFm36Output.json";
                using (var stream = await azureFileService.OpenWriteStreamAsync(messagePointer, container, new CancellationToken()))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(fm36Json);
                }

                var dto = new JobContextDto
                {
                    JobId = jobId,
                    KeyValuePairs = new Dictionary<string, object>
                    {
                        {JobContextMessageKey.CollectionYear, academicYear},
                        {JobContextMessageKey.FundingFm36Output, messagePointer},
                        {JobContextMessageKey.Filename, messagePointer},
                        {JobContextMessageKey.UkPrn, ukprn},
                        {JobContextMessageKey.Container, container},
                        {JobContextMessageKey.ReturnPeriod, collectionPeriod },
                        {JobContextMessageKey.Username, "PV2-Automated" }
                    },
                    SubmissionDateTimeUtc = DateTime.UtcNow,
                    TopicPointer = 0,
                    Topics = new List<TopicItemDto>
                    {
                        new TopicItemDto
                        {
                            SubscriptionName = "GenerateFM36Payments",
                            Tasks = new List<TaskItemDto>
                            {
                                new TaskItemDto
                                {
                                    SupportsParallelExecution = false,
                                    Tasks = new List<string>()
                                }
                            }
                        }
                    }
                };

                var publisher = topicPublishingServiceFactory.GetSubmissionPublisher(academicYear);
                await publisher.PublishAsync(dto, new Dictionary<string, object> { { "To", "GenerateFM36Payments" } }, "GenerateFM36Payments");
                await Task.Delay(500);
                dto.Topics.Add(new TopicItemDto
                {
                    SubscriptionName = "GenerateFM36Payments",
                    Tasks = new List<TaskItemDto>
                    {
                        new TaskItemDto
                        {
                            SupportsParallelExecution = false,
                            Tasks = new List<string>{"JobSuccess"}
                        }
                    }
                });
                dto.TopicPointer = 1;
                await publisher.PublishAsync(dto, new Dictionary<string, object> { { "To", "GenerateFM36Payments" } }, "GenerateFM36Payments");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}

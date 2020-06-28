using System;
using System.Runtime.CompilerServices;
using ESFA.DC.JobContext.Interface;
using ESFA.DC.Queueing;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Serialization.Interfaces;
using Microsoft.Extensions.Configuration;
using Spike.FM36Tool.Core;

namespace Spike.FM36Tool.Application.JobContext
{
    public class TopicPublishingServiceFactory
    {
        private readonly IConfiguration configuration;
        private readonly ISerializationService serializationService;
        private string servicebusConnectionString;

        public TopicPublishingServiceFactory(IConfiguration configuration, ISerializationService serializationService)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.serializationService = serializationService ?? throw new ArgumentNullException(nameof(serializationService));
            servicebusConnectionString = configuration.GetConnectionString("DcServicebusConnectionString");
        }

        public ITopicPublishService<JobContextDto> GetPeriodEndTaskPublisher(PeriodEndTask periodEndTask)
        {
            return Get("periodendtopic", "Payments");
        }

        public ITopicPublishService<JobContextDto> GetSubmissionPublisher(short academicYear)
        {
            return Get($"ilr{academicYear}submissiontopic", "GenerateFM36Payments");
        }

        private ITopicPublishService<JobContextDto> Get(string topicName, string subscriptionName)
        {
            var config = new TopicConfiguration(servicebusConnectionString,
                topicName, subscriptionName, 10, maximumCallbackTimeSpan: TimeSpan.FromMinutes(40));
            return new TopicPublishService<JobContextDto>(config, serializationService);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using ESFA.DC.ILR.FundingService.FM36.FundingOutput.Model.Output;
using Spike.FM36Tool.Application.JobContext;

namespace Spike.FM36Tool.Application.Submission
{
    public class AzureFileStorageFm36FolderSubmission
    {
        private readonly DcHelper dcHelper;
        private readonly ShareClient shareClient;

        public AzureFileStorageFm36FolderSubmission(DcHelper dcHelper, ShareClient shareClient)
        {
            this.dcHelper = dcHelper ?? throw new ArgumentNullException(nameof(dcHelper));
            this.shareClient = shareClient ?? throw new ArgumentNullException(nameof(shareClient));
        }

        public async Task<List<(long Ukprn, long JobId, int LearnerCount)>> SubmitFolder(string folderName, int academicYear, int collectionPeriod)
        {
            var folder = shareClient.GetDirectoryClient(folderName);
            var files = folder.GetFilesAndDirectories();
            var result = new List<(long Ukprn, long JobId, int LearnerCount)>();
            foreach (var shareFileItem in files)
            {
                if (shareFileItem.IsDirectory)
                    continue;
                var file = folder.GetFileClient(shareFileItem.Name);
                var download = await file.DownloadAsync();
                var fm36Json = string.Empty;
                using (var memoryStream = new MemoryStream())
                {
                    await download.Value.Content.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;
                    fm36Json = await (new StreamReader(memoryStream)).ReadToEndAsync();
                }

                var fm36 = Newtonsoft.Json.JsonConvert.DeserializeObject<FM36Global>(fm36Json);
                if (fm36 == null)
                    throw new InvalidOperationException($"Couldn't get the fm36 for file: {shareFileItem.Name} in folder: {folderName}");
                var learners = fm36?.Learners;
                var jobId = new Random(Guid.NewGuid().GetHashCode()).Next(int.MaxValue);
                await dcHelper.SubmitFM36(learners, (long)fm36.UKPRN, (short)academicYear, (byte)collectionPeriod, jobId);
                result.Add((fm36.UKPRN, jobId, learners.Count));
            }
            return result;
        }
    }
}
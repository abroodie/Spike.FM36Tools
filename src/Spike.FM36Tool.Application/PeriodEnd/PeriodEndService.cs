using System;
using System.Threading.Tasks;
using Spike.FM36Tool.Application.JobContext;
using Spike.FM36Tool.Core;

namespace Spike.FM36Tool.Application.PeriodEnd
{
    public class PeriodEndService
    {
        private readonly DcHelper dcHelper;

        public PeriodEndService(DcHelper dcHelper)
        {
            this.dcHelper = dcHelper ?? throw new ArgumentNullException(nameof(dcHelper));
        }

        public async Task <long> SendPeriodEndTask(PeriodEndTask periodEndTask, short academicYear, int collectionPeriod)
        {
            var jobId = new Random(Guid.NewGuid().GetHashCode()).Next(int.MaxValue);
            await dcHelper.SendPeriodEndTask(academicYear, (byte) collectionPeriod, jobId, periodEndTask);
            return jobId;
        }
    }
}
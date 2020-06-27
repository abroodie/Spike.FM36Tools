using Spike.FM36Tool.Core;

namespace Spike.FM36Tool.Data
{
    public class FolderSubmission
    {
        public string Name { get; set; }
        public short AcademicYear { get; set; }
        public int CollectionPeriod { get; set; } = 1;
    }
}
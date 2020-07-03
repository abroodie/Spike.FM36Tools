using System;
using System.Collections.Generic;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace Spike.FM36Tool.Application.Submission
{
    public class AzureFileStorageFM36FilesProvider
    {
        private ShareClient shareClient;
        public AzureFileStorageFM36FilesProvider(ShareClient shareClient)
        {
            this.shareClient = shareClient ?? throw new ArgumentNullException(nameof(shareClient));
        }

        public List<FileGroup> GetGroups()
        {
            var root = shareClient.GetRootDirectoryClient();
            var items = root.GetFilesAndDirectories();
            var fileGroups = new List<FileGroup>();
            foreach (ShareFileItem item in items)
            {
                if (!item.IsDirectory)
                    continue;
                var fm36Folder = root.GetSubdirectoryClient(item.Name);
                var fileGroup = new FileGroup { Name = fm36Folder.Name };
                var fm36Files = fm36Folder.GetFilesAndDirectories();
                foreach (var fm36File in fm36Files)
                {
                    if (fm36File.IsDirectory)
                        continue;
                    fileGroup.Files.Add(Fm36File.Parse(fm36File.Name));
                }
                fileGroups.Add(fileGroup);
            }

            return fileGroups;
        }

        public FileGroup GetGroup(string groupName)
        {
            var root = shareClient.GetRootDirectoryClient();
            var fm36Folder = root.GetSubdirectoryClient(groupName);
            var fileGroup = new FileGroup { Name = fm36Folder.Name };
            var fm36Files = fm36Folder.GetFilesAndDirectories();
            foreach (var fm36File in fm36Files)
            {
                if (fm36File.IsDirectory)
                    continue;
                fileGroup.Files.Add(Fm36File.Parse(fm36File.Name));
            }

            return fileGroup;
        }
    }


    public class FileGroup
    {
        public string Name { get; set; }
        public List<Fm36File> Files { get; set; } = new List<Fm36File>();
    }

    public class Fm36File
    {
        public string Name { get; set; }
        public long Ukprn { get; set; }

        public Fm36File(string name, long ukprn)
        {
            Name = name;
            Ukprn = ukprn;
        }
        public static Fm36File Parse(string filename)
        {
            var parts = filename.Split('-');
            long.TryParse(parts[1]?.Trim() ?? "0", out var ukprn);
            return new Fm36File(filename, ukprn);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FooSync
{
    public class FooSync
    {
        public const string ConfigFileName = ".FooSync_Repository.xml";
        public const string RepoStateFileName = ".FooSync_RepoState.dat";

        public FooSync()
        {
            this.Options = new Options();
        }

        public FooSync(Options options)
        {
            this.Options = options;
        }

        public FooTree Tree(string path, IEnumerable<string> exceptions)
        {
            return new FooTree(this, path, exceptions);
        }

        public FooFileInfo FileInfo(string path)
        {
            return new FooFileInfo(this, path);
        }

        public static List<string> PrepareExceptions(RepositoryDirectory dir)
        {
            var exceptions = new List<string>();

            if (dir.IgnoreRegex != null)
            {
                string pre = "", post = "";
                if (dir.IgnoreRegex.CaseInsensitive)
                {
                    pre = "(?i:";
                    post = ")";
                }

                foreach (var regex in dir.IgnoreRegex.Patterns)
                {
                    exceptions.Add(pre + regex + post);
                }
            }

            if (dir.IgnoreGlob != null)
            {
                string pre = "", post = "";
                if (dir.IgnoreGlob.CaseInsensitive)
                {
                    pre = "(?i:";
                    post = ")";
                }

                foreach (var glob in dir.IgnoreGlob.Patterns)
                {
                    exceptions.Add(pre + "^" + (Regex.Escape(glob).Replace(@"\*", ".*").Replace("?", ".")) + "$" + post);
                }
            }

            return exceptions;
        }

        public IDictionary<string, FooFileInfo> Inspect(FooTree repo, FooTree source)
        {
            var changedFiles = new Dictionary<string, FooFileInfo>();
            var repoMissingFiles = new Dictionary<string, FooFileInfo>(source.Files);

            foreach (var file in repo.Files)
            {
                file.Value.Status = FooFileInfo.ChangeStatus.Identical;

                if (source.Files.ContainsKey(file.Key))
                {
                    int comp = file.Value.CompareTo(source.Files[file.Key]);
                    if (comp == 0)
                    {
                        repoMissingFiles.Remove(file.Key);
                        continue;
                    }
                    else
                    {
                        file.Value.Status = (FooFileInfo.ChangeStatus)comp;
                    }
                }
                else
                {
                    file.Value.Status = FooFileInfo.ChangeStatus.SourceMissing;
                }

                if (file.Value.Status != FooFileInfo.ChangeStatus.Identical)
                {
                    changedFiles[file.Key] = file.Value;
                }
            }

            foreach (var file in repoMissingFiles)
            {
                changedFiles[file.Key] = file.Value;
                changedFiles[file.Key].Status = FooFileInfo.ChangeStatus.RepoMissing;
            }

            return changedFiles;
        }

        public Options Options { get; private set; }
    }
}

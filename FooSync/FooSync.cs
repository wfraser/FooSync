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
                file.Value.ChangeStatus = ChangeStatus.Identical;

                if (source.Files.ContainsKey(file.Key))
                {
                    repoMissingFiles.Remove(file.Key);
                    int comp = file.Value.CompareTo(source.Files[file.Key]);
                    if (comp == 0)
                    {
                        continue;
                    }
                    else
                    {
                        file.Value.ChangeStatus = (ChangeStatus)comp;
                    }
                }
                else
                {
                    file.Value.ChangeStatus = ChangeStatus.SourceMissing;
                }

                if (file.Value.ChangeStatus != ChangeStatus.Identical)
                {
                    changedFiles[file.Key] = file.Value;
                }
            }

            foreach (var file in repoMissingFiles)
            {
                changedFiles[file.Key] = file.Value;
                changedFiles[file.Key].ChangeStatus = ChangeStatus.RepoMissing;
            }

            return changedFiles;
        }

        public IDictionary<string, FooFileInfo> GetConflicts(IDictionary<string, FooFileInfo> changeset, RepositoryState repoState)
        {
            var conflicts = new Dictionary<string, FooFileInfo>();

            foreach (var pair in changeset)
            {
                var filename = pair.Key;
                var fileInfo = pair.Value;

                switch (fileInfo.ChangeStatus)
                {
                    case ChangeStatus.Undetermined:
                    case ChangeStatus.Identical:
                        System.Diagnostics.Debug.Assert(false, "file with invalid state in changeset");
                        break;
                    
                    case ChangeStatus.Newer:
                        if (repoState.Origin[filename] != repoState.Source.Name
                                || repoState.Repository.MTimes[filename] != fileInfo.MTime)
                                // ^ check if the recorded mtime differs from the repo's
                                // actual mtime
                        {
                            //
                            // repository file was updated independently of the source's change
                            //

                            fileInfo.ConflictStatus = ConflictStatus.RepoChanged;
                        }
                        break;

                    case ChangeStatus.Older:
                        if (repoState.Source.MTimes[filename] != fileInfo.MTime)
                        {
                            //
                            // source file was updated, then repository was updated from elsewhere
                            //

                            fileInfo.ConflictStatus = ConflictStatus.SourceChanged;
                        }
                        break;

                    case ChangeStatus.SourceMissing:
                        //
                        // Check if it was deleted (i.e. is present in the source state)
                        //
                        if (repoState.Source.MTimes.ContainsKey(filename))
                        {
                            fileInfo.ConflictStatus = ConflictStatus.DeletedInSource;
                        }
                        break;

                    case ChangeStatus.RepoMissing:
                        //
                        // Check if it was deleted (i.e. is present in the repository state)
                        //
                        if (repoState.Repository.MTimes.ContainsKey(filename))
                        {
                            fileInfo.ConflictStatus = ConflictStatus.DeletedInRepo;
                        }
                        break;
                }

                if (fileInfo.ConflictStatus != ConflictStatus.Undetermined)
                {
                    conflicts.Add(filename, fileInfo);
                }
                else
                {
                    fileInfo.ConflictStatus = ConflictStatus.NoConflict;
                }
            }

            return conflicts;
        }

        public Options Options { get; private set; }
    }

    public enum ChangeStatus
    {
        Undetermined = -2,
        Newer = -1,
        Identical = 0,
        Older = 1,
        RepoMissing = 2,
        SourceMissing = 3
    }

    public enum ConflictStatus
    {
        Undetermined,
        NoConflict,
        DeletedInSource,
        DeletedInRepo,
        RepoChanged,
        SourceChanged,
    }
}

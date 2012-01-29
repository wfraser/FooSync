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
        public static readonly TimeSpan MTimePrecision = TimeSpan.FromSeconds(1);

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

        public IDictionary<string, FooFileInfo> Inspect(FooTree repo, FooTree source, RepositoryState state)
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
                    if (state.Source.MTimes.ContainsKey(file.Key))
                    {
                        file.Value.ChangeStatus = ChangeStatus.SourceDeleted;
                    }
                    else
                    {
                        file.Value.ChangeStatus = ChangeStatus.SourceMissing;
                    }
                }

                if (file.Value.ChangeStatus != ChangeStatus.Identical)
                {
                    changedFiles[file.Key] = file.Value;
                }
            }

            foreach (var file in repoMissingFiles)
            {
                changedFiles[file.Key] = file.Value;

                if (state.Repository.MTimes.ContainsKey(file.Key))
                {
                    changedFiles[file.Key].ChangeStatus = ChangeStatus.RepoDeleted;
                }
                else
                {
                    changedFiles[file.Key].ChangeStatus = ChangeStatus.RepoMissing;
                }
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

                    case ChangeStatus.SourceDeleted:
                        //
                        // Check if the file is new in repository,
                        //  or repository file's mtime differs from state
                        //
                        if (!repoState.Repository.MTimes.ContainsKey(filename) 
                                || !DateTimesWithinPrecision(fileInfo.MTime, repoState.Repository.MTimes[filename], MTimePrecision))
                        {
                            fileInfo.ConflictStatus = ConflictStatus.ChangedInRepoDeletedInSource;
                        }
                        break;

                    case ChangeStatus.RepoMissing:
                        //
                        // Check if the file is new in source,
                        //  or source file's mtime differs from state
                        //
                        if (!repoState.Source.MTimes.ContainsKey(filename)
                                || !DateTimesWithinPrecision(fileInfo.MTime, repoState.Source.MTimes[filename], MTimePrecision))
                        {
                            fileInfo.ConflictStatus = ConflictStatus.ChangedInSourceDeletedInRepo;
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

        private static bool DateTimesWithinPrecision(DateTime a, DateTime b, TimeSpan precision)
        {
            DateTime a_clipped = new DateTime(a.Ticks - (a.Ticks % precision.Ticks));
            DateTime b_clipped = new DateTime(b.Ticks - (b.Ticks % precision.Ticks));

            return (a_clipped == b_clipped);
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
        SourceMissing = 3,
        RepoDeleted = 4,
        SourceDeleted = 5,
    }

    public enum ConflictStatus
    {
        Undetermined,
        NoConflict,
        ChangedInRepoDeletedInSource,
        ChangedInSourceDeletedInRepo,
        RepoChanged,
        SourceChanged,
    }
}

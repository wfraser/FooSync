﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FooSync
{
    public class FooSyncEngine
    {
        public const string ConfigFileName = ".FooSync_Repository.xml";
        public const string RepoStateFileName = ".FooSync_RepoState.dat";
        public static readonly TimeSpan MTimePrecision = TimeSpan.FromSeconds(1);

        public FooSyncEngine()
        {
            this.Options = new Options();
        }

        public FooSyncEngine(Options options)
        {
            this.Options = options;
        }

        public FooTree Tree(string path, IEnumerable<string> exceptions, Progress callback = null)
        {
            return new FooTree(this, path, exceptions, callback);
        }

        public FooFileInfo FileInfo(string path)
        {
            return new FooFileInfo(this, path);
        }

        public FooChangeSet ChangeSet()
        {
            return new FooChangeSet(this);
        }

        public static ICollection<string> PrepareExceptions(RepositoryDirectory dir)
        {
            if (dir == null)
                throw new ArgumentNullException("dir");

            var exceptions = new List<string>();

            if (dir.IgnoreRegex != null && dir.IgnoreRegex.Patterns != null && dir.IgnoreRegex.Patterns.Length > 0)
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

            if (dir.IgnoreGlob != null && dir.IgnoreGlob.Patterns != null && dir.IgnoreGlob.Patterns.Length > 0)
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822: Mark members as static")]
        public FooChangeSet Inspect(RepositoryState state, FooTree repo, FooTree source, Progress callback = null)
        {
            if (repo == null)
                throw new ArgumentNullException("repo");
            if (source == null)
                throw new ArgumentNullException("source");
            if (state == null)
                throw new ArgumentNullException("state");

            var changeset = this.ChangeSet();
            var repoMissingFiles = new HashSet<string>(source.Files.Keys);

            int n = 0;
            foreach (var file in repo.Files)
            {
                var filename = file.Key;
                ChangeStatus status = ChangeStatus.Identical;

                if (callback != null)
                {
                    callback(n++, repo.Files.Count + repoMissingFiles.Count, Path.GetDirectoryName(filename));
                }

                if (source.Files.ContainsKey(filename))
                {
                    repoMissingFiles.Remove(filename);
                    int comp = file.Value.CompareTo(source.Files[filename]);
                    status = (ChangeStatus)comp;
                }
                else
                {
                    if (state.Source.MTimes.ContainsKey(filename))
                    {
                        status = ChangeStatus.SourceDeleted;
                    }
                    else
                    {
                        status = ChangeStatus.SourceMissing;
                    }
                }

                if (status != ChangeStatus.Identical)
                {
                    changeset.Add(filename, status);
                }
            }
            
            foreach (var filename in repoMissingFiles)
            {
                ChangeStatus status = ChangeStatus.Undetermined;

                if (callback != null)
                {
                    callback(n++, repo.Files.Count + repoMissingFiles.Count, Path.GetDirectoryName(filename));
                }

                if (state.Repository.MTimes.ContainsKey(filename))
                {
                    status = ChangeStatus.RepoDeleted;
                }
                else
                {
                    status = ChangeStatus.RepoMissing;
                }

                changeset.Add(filename, status);
            }

            return changeset;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822: Mark members as static")]
        public void GetConflicts(FooChangeSet changeset, RepositoryState repoState, FooTree repo, FooTree source)
        {
            if (changeset == null)
                throw new ArgumentNullException("changeset");
            if (repoState == null)
                throw new ArgumentNullException("repoState");
            if (repo == null)
                throw new ArgumentNullException("repo");
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var filename in changeset)
            {
                switch (changeset[filename].ChangeStatus)
                {
                    case ChangeStatus.Undetermined:
                    case ChangeStatus.Identical:
                        System.Diagnostics.Debug.Assert(false, "file with invalid state in changeset");
                        break;
                    
                    case ChangeStatus.Newer:
                        if (!repoState.Origin.ContainsKey(filename) || !repoState.Repository.MTimes.ContainsKey(filename)
                                || (repoState.Origin[filename] != repoState.Source.Name
                                        && !DateTimesWithinPrecision(repoState.Repository.MTimes[filename], repo.Files[filename].MTime, MTimePrecision)))
                        {
                            //
                            // repository file was updated independently of the source's change
                            //

                            changeset[filename].ConflictStatus = ConflictStatus.RepoChanged;
                        }
                        break;

                    case ChangeStatus.Older:
                        if (!repoState.Source.MTimes.ContainsKey(filename)
                                || !DateTimesWithinPrecision(repoState.Source.MTimes[filename], source.Files[filename].MTime, MTimePrecision))
                        {
                            //
                            // source file was updated, then repository was updated from elsewhere
                            //

                            changeset[filename].ConflictStatus = ConflictStatus.SourceChanged;
                        }
                        break;

                    case ChangeStatus.SourceDeleted:
                        //
                        // Check if repository file's mtime differs from state
                        //
                        if (!DateTimesWithinPrecision(repo.Files[filename].MTime, repoState.Repository.MTimes[filename], MTimePrecision))
                        {
                            changeset[filename].ConflictStatus = ConflictStatus.ChangedInRepoDeletedInSource;
                        }
                        break;

                    case ChangeStatus.RepoDeleted:
                        //
                        // Check if the file is new in source,
                        //  or source file's mtime differs from state
                        //
                        if (!DateTimesWithinPrecision(source.Files[filename].MTime, repoState.Source.MTimes[filename], MTimePrecision))
                        {
                            changeset[filename].ConflictStatus = ConflictStatus.ChangedInSourceDeletedInRepo;
                        }
                        break;
                }

                if (changeset[filename].ConflictStatus == ConflictStatus.Undetermined)
                {
                    changeset[filename].ConflictStatus = ConflictStatus.NoConflict;
                }
            }
        }

        public void SetDefaultActions(FooChangeSet changeset)
        {
            foreach (var filename in changeset)
            {
                if (changeset[filename].ConflictStatus == ConflictStatus.NoConflict)
                {
                    switch (changeset[filename].ChangeStatus)
                    {
                        case ChangeStatus.Newer:
                        case ChangeStatus.RepoMissing:
                            changeset[filename].FileOperation = FileOperation.UseSource;
                            break;

                        case ChangeStatus.Older:
                        case ChangeStatus.SourceMissing:
                            changeset[filename].FileOperation = FileOperation.UseRepo;
                            break;

                        case ChangeStatus.RepoDeleted:
                            changeset[filename].FileOperation = FileOperation.DeleteSource;
                            break;

                        case ChangeStatus.SourceDeleted:
                            changeset[filename].FileOperation = FileOperation.DeleteRepo;
                            break;

                        default:
                            System.Diagnostics.Debug.Assert(false, "Invalid change status!");
                            break;
                    }
                }
            }
        }

        private static bool DateTimesWithinPrecision(DateTime a, DateTime b, TimeSpan precision)
        {
            DateTime a_clipped = new DateTime(a.Ticks - (a.Ticks % precision.Ticks));
            DateTime b_clipped = new DateTime(b.Ticks - (b.Ticks % precision.Ticks));

            return (a_clipped == b_clipped);
        }

        public Options Options { get; private set; }
    }

    public delegate void Progress(int completed, int total, string item);

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

    public enum FileOperation
    {
        NoOp,
        UseRepo,
        UseSource,
        DeleteRepo,
        DeleteSource,
        MaxFileOperation
    }
}

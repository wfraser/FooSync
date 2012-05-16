///
/// Codewise/FooSync/FooSync.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Codewise.FooSync
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

        //public static ICollection<string> PrepareExceptions(RepositoryDirectory dir)
        public static ICollection<string> PrepareExceptions(IgnorePatterns ignoreRegex, IgnorePatterns ignoreGlob)
        {
            var exceptions = new List<string>();

            if (ignoreRegex != null && ignoreRegex.Patterns != null && ignoreRegex.Patterns.Length > 0)
            {
                string pre = string.Empty, post = string.Empty;
                if (ignoreRegex.CaseInsensitive)
                {
                    pre = "(?i:";
                    post = ")";
                }

                foreach (var regex in ignoreRegex.Patterns)
                {
                    exceptions.Add(pre + regex + post);
                }
            }

            if (ignoreGlob != null && ignoreGlob.Patterns != null && ignoreGlob.Patterns.Length > 0)
            {
                string pre = string.Empty, post = string.Empty;
                if (ignoreGlob.CaseInsensitive)
                {
                    pre = "(?i:";
                    post = ")";
                }

                foreach (var glob in ignoreGlob.Patterns)
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

            var changeset = new FooChangeSet();
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
        public static void GetConflicts(FooChangeSet changeset, RepositoryState repoState, FooTree repo, FooTree source)
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
                        if (!repoState.Repository.MTimes.ContainsKey(filename)
                                || !DateTimesWithinPrecision(repo.Files[filename].MTime, repoState.Repository.MTimes[filename], MTimePrecision))
                        {
                            changeset[filename].ConflictStatus = ConflictStatus.ChangedInRepoDeletedInSource;
                        }
                        break;

                    case ChangeStatus.RepoDeleted:
                        //
                        // Check if the file is new in source,
                        //  or source file's mtime differs from state
                        //
                        if (!repoState.Source.MTimes.ContainsKey(filename)
                                || !DateTimesWithinPrecision(source.Files[filename].MTime, repoState.Source.MTimes[filename], MTimePrecision))
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

        public static void UpdateRepoState(RepositoryState state, FooChangeSet changeset, FooTree repo, FooTree source)
        {
            if (state == null)
                throw new ArgumentNullException("state");
            if (changeset == null)
                throw new ArgumentNullException("changeset");
            if (repo == null)
                throw new ArgumentNullException("repo");
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var filename in changeset.Where(e => e.FileOperation != FileOperation.NoOp))
            {
                ChangeStatus cstatus = changeset[filename].ChangeStatus;
                FileOperation operation = changeset[filename].FileOperation;

                if (cstatus == ChangeStatus.SourceDeleted
                        && operation != FileOperation.UseRepo)
                {
                    state.Source.MTimes.Remove(filename);
                }

                if (cstatus == ChangeStatus.RepoDeleted
                        && operation != FileOperation.UseSource)
                {
                    state.Repository.MTimes.Remove(filename);
                }

                if (operation == FileOperation.UseSource)
                {
                    state.Repository.MTimes[filename] = source.Files[filename].MTime;
                    state.Source.MTimes[filename] = source.Files[filename].MTime;
                    state.Origin[filename] = state.Source.Name;
                }
                else if (operation == FileOperation.UseRepo)
                {
                    state.Repository.MTimes[filename] = repo.Files[filename].MTime;
                    state.Source.MTimes[filename] = repo.Files[filename].MTime;
                }
                else if (operation == FileOperation.DeleteSource)
                {
                    state.Source.MTimes.Remove(filename);
                }
                else if (operation == FileOperation.DeleteRepo)
                {
                    state.Repository.MTimes.Remove(filename);
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

    public enum OpCode : int
    {
        Hello = 0,
        Auth,
        ListRepos,
        Tree,
        State,
        GetFile,
        PutFile,
        DelFile,
        HttpGet =  0x20544547, // == "GET "
        HttpPost = 0x54534F50, // == "POST"
    }

    public enum RetCode : int
    {
        Success,
        UnknownError,
        BadAuth,
        BadRepo,
        BadPath,
        BadOp
    }
}

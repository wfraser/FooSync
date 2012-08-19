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
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Codewise.FooSync
{
    public class FooSyncEngine
    {
        public const string RepoStateFileName = ".FooSync_Repository.dat";
        public static readonly TimeSpan MTimePrecision = TimeSpan.FromSeconds(1);

        public FooSyncEngine()
        {
            this.Options = new Options();
        }

        public FooSyncEngine(Options options)
        {
            this.Options = options;
        }

        public static ICollection<string> PrepareExceptions(ICollection<IIgnorePattern> ignorePatterns)
        {
            var exceptions = new List<string>();

            string pre = "(?i:";
            string post = ")";

            if (ignorePatterns != null && ignorePatterns.Count > 0)
            {
                foreach (var pattern in ignorePatterns)
                {
                    exceptions.Add((pattern.CaseInsensitive ? pre
                                                            : string.Empty)
                                   + (pattern.IsRegex ? pattern.Pattern
                                                      : "^" + (Regex.Escape(pattern.Pattern).Replace(@"\*", ".*").Replace("?", ".")) + "$")
                                   + (pattern.CaseInsensitive ? post
                                                              : string.Empty) );
                }
            }

            return exceptions;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822: Mark members as static")]
        public FooChangeSet Inspect(RepositoryStateCollection state, Dictionary<Guid, FooTree> trees, Progress callback = null)
        {
            if (state == null)
                throw new ArgumentNullException("state");
            if (trees == null)
                throw new ArgumentNullException("trees");

            FooChangeSet changeset = new FooChangeSet();

            long total = (from tree in trees.Values
                          select tree.Files.Count)
                            .Aggregate((a, b) => (a + b));
            long current = 0;

            foreach (Guid repoId in trees.Keys)
            {
                foreach (string filename in trees[repoId].Files.Keys)
                {
                    if (callback != null)
                    {
                        callback(current++, total, trees[repoId].Base.IsLocal ? Path.Combine(trees[repoId].Base.LocalPath, filename)
                                                                              : trees[repoId].Base.ToString() + filename);
                    }

                    if (state.Repositories[repoId].MTimes.ContainsKey(filename))
                    {
                        foreach (Guid otherId in trees.Keys)
                        {
                            if (repoId == otherId)
                            {
                                continue;
                            }
                            if (trees[otherId].Files.ContainsKey(filename))
                            {
                                DateTime repoTime = trees[repoId].Files[filename].MTime;
                                DateTime otherTime = trees[otherId].Files[filename].MTime;

                                if (state.Repositories[repoId].MTimes[filename] != repoTime)
                                {
                                    changeset.Add(filename, ChangeStatus.Changed, repoId);
                                }

                                if (state.Repositories[otherId].MTimes[filename] != otherTime)
                                {
                                    changeset.Add(filename, ChangeStatus.Changed, otherId);
                                }
                            }
                            else
                            {
                                //
                                // Don't check if the file exists in the state for the other repo; 
                                // just display it as missing.
                                // 

                                changeset.Add(filename, ChangeStatus.Missing, otherId);
                            }
                        }
                    }
                    else
                    {
                        changeset.Add(filename, ChangeStatus.New, repoId);
                    }
                }
            }

            FooChangeSet newChanges = new FooChangeSet();
            foreach (string filename in changeset.Filenames)
            {
                foreach (FooChangeSetElem change in changeset[filename].Values)
                {
                    switch (change.ChangeStatus)
                    {
                        case ChangeStatus.Changed:
                        case ChangeStatus.Deleted:
                        case ChangeStatus.Missing:
                            //TODO
                            break;

                        case ChangeStatus.New:
                            var treesWithThisFile = trees.Where(pair => pair.Value.Files.ContainsKey(filename) && pair.Key != change.RepositoryId);
                            if (treesWithThisFile.Count() == 0)
                            {
                                foreach (Guid repoId in trees.Keys.Where(id => id != change.RepositoryId))
                                {
                                    newChanges.Add(filename, ChangeStatus.Missing, repoId);
                                }
                            }
                            else
                            {
                                //todo
                            }
                            break;
                    }
                }
            }

            changeset.UnionWith(newChanges);

            return changeset;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822: Mark members as static")]
        public static void GetConflicts(FooChangeSet changeset, RepositoryStateCollection repoState, FooTree repo, FooTree source)
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
                /*
                switch (changeset[filename].ChangeStatus)
                {
                    case ChangeStatus.Undetermined:
                    case ChangeStatus.Identical:
                        System.Diagnostics.Debug.Assert(false, "file with invalid state in changeset");
                        break;
                    
                    case ChangeStatus.Newer:
                        if (!repoState.Origin.ContainsKey(filename) || !repoState.Repository.MTimes.ContainsKey(filename)
                                || (repoState.Origin[filename] != repoState.Source.ID
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
                
                 */
            }
        }

        public static void UpdateRepoState(RepositoryStateCollection state, FooChangeSet changeset, FooTree repo, FooTree source)
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
                /*
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
                    state.Origin[filename] = state.Source.ID;
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
                 */
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

    public interface IIgnorePattern
    {
        string Pattern { get; set; }
        bool CaseInsensitive { get; set; }
        bool IsRegex { get; set; }
    }

    public delegate void Progress(long completed, long total, string item);

    public enum ChangeStatus
    {
        Undetermined,
        Identical,
        Changed,
        Missing,
        Deleted,
        New,
    }

    public enum ConflictStatus
    {
        Undetermined,
        NoConflict,
        ChangedAndDeleted,
        MultipleChanges,
    }

    public enum FileOperation
    {
        NoOp,
        Give,
        Take,
        Delete,
        DeleteOthers,
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
        InternalError,
        BadAuth,
        BadRepo,
        BadPath,
        BadOp
    }
}

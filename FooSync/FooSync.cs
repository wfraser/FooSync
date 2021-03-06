﻿///
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
using System.ComponentModel;
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

            FooChangeSet changeset = new FooChangeSet(trees.Keys);

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

                                changeset.Add(
                                    filename,
                                    (state.Repositories[repoId].MTimes[filename] == repoTime)
                                        ? ChangeStatus.Identical
                                        : ChangeStatus.Changed,
                                    repoId);

                                changeset.Add(
                                    filename,
                                    (state.Repositories[otherId].MTimes[filename] == otherTime)
                                        ? ChangeStatus.Identical
                                        : ChangeStatus.Changed,
                                    otherId);
                            }
                            else
                            {
                                //
                                // Don't check if the file exists in the state for the other repo; 
                                // just display it as missing.
                                // 

                                changeset.Add(filename, ChangeStatus.Missing, otherId);

                                //
                                // Also add an 'Identical' entry for the existing one.
                                //

                                changeset.Add(filename, ChangeStatus.Identical, repoId);
                            }
                        }
                    }
                    else
                    {
                        changeset.Add(filename, ChangeStatus.New, repoId);
                    }
                }
            }

            foreach (string filename in changeset.Filenames)
            {
                FooChangeSetElem change = changeset[filename];

                foreach (Guid repoId in trees.Keys)
                {
                    if (change.ChangeStatus[repoId] == ChangeStatus.Undetermined)
                    {
                        changeset[filename].ChangeStatus[repoId] =
                            (state.Repositories.ContainsKey(repoId) && state.Repositories[repoId].MTimes.ContainsKey(filename))
                                ? ChangeStatus.Deleted
                                : ChangeStatus.Missing;
                    }
                }
            }

            return changeset;
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

            /*
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
            }
            */
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
        [Description("Undetermined")]   Undetermined,
        [Description("Identical")]      Identical,
        [Description("Changed")]        Changed,
        [Description("Missing")]        Missing,
        [Description("Deleted")]        Deleted,
        [Description("New")]            New,
    }

    public enum ConflictStatus
    {
        [Description("Undetermined")]           Undetermined,
        [Description("No Conflict")]            NoConflict,
        [Description("Changed and Deleted")]    ChangedAndDeleted,
        [Description("Multiple Changes")]       MultipleChanges,
    }

    public enum FileOperation
    {
        [Description("Ignore")]         NoOp,
        [Description("Source")]         Source,
        [Description("Destination")]    Destination,
        [Description("Delete")]         Delete,
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using FooSync;

namespace FooSync.ConsoleApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var fooOptions = new Options();
            var program = new Program(fooOptions);
            program.Run(args);
        }

        Program(Options fooOptions)
        {
            this.Foo = new FooSync(fooOptions);
        }

        void Run(string[] args)
        {
            Console.WriteLine("FooSync.ConsoleApp v{0} / FooSync v{1}",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                System.Reflection.Assembly.GetAssembly(Foo.GetType()).GetName().Version);

            Console.WriteLine("{0} / {1} / {2}",
                Environment.MachineName,
                Environment.OSVersion.Platform,
                Environment.OSVersion.VersionString);
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }
            Console.WriteLine();

            //
            // Load the repository config
            //

            string repoConfigError;
            RepositoryConfig config = RepositoryConfigLoader.GetRepositoryConfig(FooSync.ConfigFileName, out repoConfigError);

            if (config == null)
            {
                Console.WriteLine("There's a problem with your config file: {0}", repoConfigError);
                return;
            }

            foreach (var dir in config.Directories)
            {
                if (dir.Source == null)
                {
                    Console.WriteLine("There's no entry matching your machine name ({0}) in the "
                        + "repository configuration file for the directory \"{1}\". Skipping.",
                        Environment.MachineName.ToLower(),
                        dir.Path);
                    continue;
                }

                var exceptions = FooSync.PrepareExceptions(dir);

                //
                // Enumerate files in the source and repository trees
                //

                FooTree repo = GetFooTree(dir.Path, exceptions, "repository");
                if (repo == null)
                    return;

                FooTree source = GetFooTree(dir.Source.Path, exceptions, "source");
                if (source == null)
                    return;

                //
                // Compute & display the change set
                //

                var changedFiles = Foo.Inspect(repo, source);

                if (changedFiles.Count == 0)
                {
                    Console.WriteLine("No changes; nothing to do.");
                    return;
                }

                Console.WriteLine("File changes:");
                foreach (var file in changedFiles)
                {
                    string descr = string.Empty;
                    switch (file.Value.ChangeStatus)
                    {
                        case ChangeStatus.Identical:
                        case ChangeStatus.Undetermined:
                            throw new ApplicationException("Bogus file change state");

                        case ChangeStatus.Newer:
                            descr = "Newer than the repository";
                            break;

                        case ChangeStatus.Older:
                            descr = "Older than the repository";
                            break;

                        case ChangeStatus.RepoMissing:
                            descr = "Not in the repository";
                            break;

                        case ChangeStatus.SourceMissing:
                            descr = "Not in our directory";
                            break;
                    }

                    Console.WriteLine("\t{0}: {1}", descr, file.Key);
                }

                //
                // Load / generate the repository state
                //
                
                RepositoryState state;
                try
                {
                    state = new RepositoryState(FooSync.RepoStateFileName);
                }
                catch (FileNotFoundException)
                {
                    state = new RepositoryState();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unexpected {0} trying to read repository state: {1}",
                        ex.GetType().Name,
                        ex.Message);
                    return;
                }

                bool stateChanged = false;
                if (state.Repository == null)
                {
                    state.AddSource(repo, RepositoryState.RepoSourceName);
                    stateChanged = true;
                }

                if (!state.Sources.ContainsKey(Environment.MachineName.ToLower()))
                {
                    state.AddSource(source, Environment.MachineName.ToLower());
                    stateChanged = true;
                }

                if (stateChanged)
                {
                    state.Write(FooSync.RepoStateFileName);
                }

                //
                // Check against the repository state
                //

                var conflicts = Foo.GetConflicts(changedFiles, state);
                var copyOperations = new List<KeyValuePair<string, CopyOperation>>();

                foreach (var change in changedFiles)
                {
                    if (change.Value.ConflictStatus == ConflictStatus.NoConflict)
                    {
                        switch (change.Value.ChangeStatus)
                        {
                            case ChangeStatus.Newer:
                            case ChangeStatus.RepoMissing:
                                copyOperations.Add(new KeyValuePair<string, CopyOperation>(change.Key, CopyOperation.UseSource));
                                break;

                            case ChangeStatus.Older:
                            case ChangeStatus.SourceMissing:
                                copyOperations.Add(new KeyValuePair<string, CopyOperation>(change.Key, CopyOperation.UseRepo));
                                break;

                            default:
                                throw new ApplicationException("Error, invalid change status!");
                        }
                    }
                    // else: no-op, these are handled by the conflicts loop below
                }

                if (conflicts.Count > 0) {
                    Console.WriteLine("\n{0} Conflicts:\n", conflicts.Count);
                }
                foreach (var conflict in conflicts)
                {
                    string descr = string.Empty;
                    switch (conflict.Value.ConflictStatus)
                    {
                        case ConflictStatus.DeletedInRepo:
                            descr = "File changed in the source and deleted in the repository";
                            break;

                        case ConflictStatus.DeletedInSource:
                            descr = "File changed in the repository and deleted in the source";
                            break;

                        case ConflictStatus.RepoChanged:
                            descr = "File changed in the repository and in the source (source newer)";
                            break;

                        case ConflictStatus.SourceChanged:
                            descr = "File changed in the repository and in the source (repository newer)";
                            break;
                    }

                    Console.WriteLine("{0}:\n{1}", descr, conflict.Key);
                    if (conflict.Value.ConflictStatus != ConflictStatus.DeletedInSource)
                    {
                        Console.WriteLine("\tSource:     modified {0}, {1} bytes",
                            source.Files[conflict.Key].MTime.ToString(),
                            source.Files[conflict.Key].Size);
                    }
                    if (conflict.Value.ConflictStatus != ConflictStatus.DeletedInRepo)
                    {
                        Console.WriteLine("\tRepository: modified {0}, {1} bytes",
                            repo.Files[conflict.Key].MTime.ToString(),
                            repo.Files[conflict.Key].Size);
                    }

                    KeyValuePair<string, CopyOperation>? action = GetActionFromUser(conflict.Key);
                    if (action.HasValue)
                    {
                        copyOperations.Add(action.Value);
                    }
                }

                //
                // Display overview of actions to be taken
                //

                bool accepted = false;
                while (!accepted)
                {
                    Console.WriteLine("\nActions to be taken:\n");

                    int n = 1;
                    foreach (var change in copyOperations)
                    {
                        string sfile = Path.GetFullPath(Path.Combine(source.Path, change.Key));
                        string rfile = Path.GetFullPath(Path.Combine(repo.Path, change.Key));
                        string num = string.Format("{0,"
                            + Math.Ceiling(Math.Log10(copyOperations.Count)).ToString()
                            + "}",
                            n++);

                        bool useRepo = (change.Value == CopyOperation.UseRepo);

                        Console.WriteLine("{0}: {1}\n    -> {2}",
                            num,
                            useRepo ? rfile : sfile,
                            useRepo ? sfile : rfile);
                    }

                    Console.Write("\nPress Enter to perform actions, or enter a number to edit one: ");
                    string changeNum = Console.ReadLine();

                    if (changeNum == string.Empty)
                    {
                        accepted = true;
                    }
                    else
                    {
                        try
                        {
                            n = int.Parse(changeNum) - 1;
                            if (n < 0 || n >= copyOperations.Count)
                                throw new FormatException("out of range");

                            string path = copyOperations[n].Key;

                            var action = GetActionFromUser(path);
                            if (action.HasValue)
                            {
                                copyOperations[n] = action.Value;
                            }
                            else
                            {
                                copyOperations.RemoveAt(n);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is FormatException
                                || ex is OverflowException)
                            {
                                Console.WriteLine("invalid selection");
                            }
                            else
                            {
                                throw ex;
                            }
                        }
                    }
                }

                //
                // Perform the operations
                //

                if (copyOperations.Count == 0)
                {
                    Console.WriteLine("Nothing to do.");
                    return;
                }

                var srcFiles = new List<string>();
                var dstFiles = new List<string>();

                foreach (var c in copyOperations)
                {
                    string srcFile, dstFile;
                    if (c.Value == CopyOperation.UseRepo)
                    {
                        srcFile = Path.GetFullPath(Path.Combine(repo.Path, c.Key));
                        dstFile = Path.GetFullPath(Path.Combine(source.Path, c.Key));
                    }
                    else
                    {
                        dstFile = Path.GetFullPath(Path.Combine(repo.Path, c.Key));
                        srcFile = Path.GetFullPath(Path.Combine(source.Path, c.Key));
                    }

                    srcFiles.Add(srcFile);
                    dstFiles.Add(dstFile);
                }

                Console.Write("Copying files...");
                CopyEngine.Copy(srcFiles, dstFiles);
                Console.WriteLine(" done.");

                //TODO: check whether the file copy succeeded and all files were copied

                //
                // Update the repository state
                //

                foreach (var c in copyOperations)
                {
                    if (c.Value == CopyOperation.UseSource)
                    {
                        state.Origin[c.Key] = state.Source.Name;
                    }
                }

                state.Write(FooSync.RepoStateFileName);
            }
        }

        private FooTree GetFooTree(string path, List<string> exceptions, string type)
        {
            FooTree ret = null;
            try
            {
                ret = Foo.Tree(path, exceptions);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("{0} directory {1} not found.",
                    type,
                    path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} reading {1} directory {2}: {3}",
                    ex.GetType().Name,
                    type,
                    path,
                    ex.Message);
            }

            return ret;
        }

        private KeyValuePair<string, CopyOperation>? GetActionFromUser(string path)
        {
            KeyValuePair<string, CopyOperation>? ret = null;

            bool actionAccepted = false;
            while (!actionAccepted)
            {
                Console.Write("Action: Take (R)epository | Take (S)ource | Do (N)othing : ");
                var key = Console.ReadKey(false);
                actionAccepted = true;
                switch (key.Key)
                {
                    case ConsoleKey.R:
                        ret = new KeyValuePair<string, CopyOperation>(path, CopyOperation.UseRepo);
                        break;

                    case ConsoleKey.S:
                        ret = new KeyValuePair<string, CopyOperation>(path, CopyOperation.UseSource);
                        break;

                    case ConsoleKey.N:
                        ret = null;
                        break;

                    default:
                        Console.WriteLine("Invalid key pressed.");
                        actionAccepted = false;
                        break;
                }
            }

            return ret;
        }

        private FooSync Foo { get; set; }

        private enum CopyOperation
        {
            UseRepo,
            UseSource
        }
    }
}

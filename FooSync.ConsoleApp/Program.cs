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
            var programArgs = new ProgramArguments(args);
            var fooOptions = new Options();

            if (programArgs.Flags.ContainsKey("hash"))
            {
                fooOptions.ComputeHashes = programArgs.Flags["hash"];
            }

            if (programArgs.Flags.ContainsKey("casesensitive"))
            {
                fooOptions.CaseInsensitive = !programArgs.Flags["casesensitive"];
            }
            else
            {
                //
                // default to case-sensitive on Unix
                //
                fooOptions.CaseInsensitive = !(Environment.OSVersion.Platform == PlatformID.Unix);
            }

            var foo = new FooSyncEngine(fooOptions);
            
            Console.WriteLine("FooSync.ConsoleApp v{0} / FooSync v{1}",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                foo.GetType().Assembly.GetName().Version.ToString());

            Console.WriteLine("{0} / {1} / {2}",
                Environment.MachineName,
                Environment.OSVersion.Platform,
                Environment.OSVersion.VersionString);
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }
            Console.WriteLine();

            if (programArgs.Flags.ContainsKey("help"))
            {
                Console.WriteLine("usage: {0} [options]", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                Console.WriteLine("Loads its configuration from {0} in the current directory", FooSyncEngine.ConfigFileName);
                return;
            }

            var program = new Program(foo);
            program.Run(programArgs);
        }

        Program(FooSyncEngine foo)
        {
            this.Foo = foo;
        }

        void Run(ProgramArguments args)
        {
            //
            // Load the repository config
            //

            string repoConfigError;
            RepositoryConfig config = RepositoryConfigLoader.GetRepositoryConfig(FooSyncEngine.ConfigFileName, out repoConfigError);

            if (config == null)
            {
                Console.WriteLine("There's a problem with your config file: {0}", repoConfigError);
                return;
            }


            List<RepositoryDirectory> directories;
            if (args.Options.ContainsKey("directory"))
            {
                directories = new List<RepositoryDirectory>();
                foreach (var dir in config.Directories)
                {
                    if (dir.Path == args.Options["directory"])
                    {
                        directories.Add(dir);
                        break;
                    }
                }
            }
            else
            {
                directories = new List<RepositoryDirectory>(config.Directories);
            }

            foreach (var dir in directories)
            {
                Console.WriteLine("Synchronizing directory {0}:", dir.Path);

                if (dir.Source == null)
                {
                    Console.WriteLine("There's no entry matching your machine name ({0}) in the "
                        + "repository configuration file for the directory \"{1}\". Skipping.",
                        Environment.MachineName.ToLower(),
                        dir.Path);
                    continue;
                }

                var exceptions = FooSyncEngine.PrepareExceptions(dir);

                //
                // Enumerate files in the source and repository trees
                //

                Console.Write("Enumerating files: repository...");
                FooTree repo = GetFooTree(dir.Path, exceptions, "repository");
                if (repo == null)
                    continue;

                Console.Write(" done. Source...");
                
                FooTree source = GetFooTree(dir.Source.Path, exceptions, "source");
                if (source == null)
                    continue;

                Console.Write(" done.\n");


                //
                // Load / generate the repository state
                //

                Console.Write("Loading repository state...");
                RepositoryState state;
                try
                {
                    state = new RepositoryState(Path.Combine(dir.Path, FooSyncEngine.RepoStateFileName));
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
                    state.Write(Path.Combine(dir.Path, FooSyncEngine.RepoStateFileName));
                }
                Console.Write(" done.\n");

                //
                // Compute & display the change set
                //

                Console.Write("Comparing files...");
                var changeset = Foo.Inspect(repo, source, state);
                Console.Write(" done.\n");

                if (changeset.Count(e => e.ChangeStatus != ChangeStatus.Identical) == 0)
                {
                    Console.WriteLine("No changes; nothing to do.\n");
                    continue;
                }

                //
                // Check against the repository state
                //

                Foo.GetConflicts(changeset, state, repo, source);

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
                                Debug.Assert(false, "Invalid change status!");
                                break;
                        }
                    }
                    // else: no-op, these are handled by the conflicts loop below
                }

                int conflictCount = changeset.Count(e => e.ConflictStatus != ConflictStatus.NoConflict);
                if (conflictCount > 0) {
                    Console.WriteLine("\n{0} Conflicts:\n", conflictCount);

                    foreach (var filename in changeset.Conflicts)
                    {
                        string descr = string.Empty;
                        switch (changeset[filename].ConflictStatus)
                        {
                            case ConflictStatus.ChangedInSourceDeletedInRepo:
                                descr = "File changed in the source and deleted in the repository";
                                break;

                            case ConflictStatus.ChangedInRepoDeletedInSource:
                                descr = "File changed in the repository and deleted in the source";
                                break;

                            case ConflictStatus.RepoChanged:
                                descr = "File changed in the repository and in the source (source newer)";
                                break;

                            case ConflictStatus.SourceChanged:
                                descr = "File changed in the repository and in the source (repository newer)";
                                break;
                        }

                        Console.WriteLine("{0}:\n{1}", descr, filename);
                        if (changeset[filename].ConflictStatus != ConflictStatus.ChangedInRepoDeletedInSource)
                        {
                            Console.WriteLine("\tSource:     modified {0}, {1} bytes",
                                source.Files[filename].MTime.ToString(),
                                source.Files[filename].Size);
                        }
                        if (changeset[filename].ConflictStatus != ConflictStatus.ChangedInSourceDeletedInRepo)
                        {
                            Console.WriteLine("\tRepository: modified {0}, {1} bytes",
                                repo.Files[filename].MTime.ToString(),
                                repo.Files[filename].Size);
                        }

                        string rpath = Path.GetFullPath(Path.Combine(repo.Path, filename));
                        string spath = Path.GetFullPath(Path.Combine(source.Path, filename));

                        FileOperation action = GetActionFromUser(File.Exists(rpath) ? rpath : null, File.Exists(spath) ? spath : null);
                        changeset[filename].FileOperation = action;
                        Console.WriteLine();
                    }
                }

                //
                // Display overview of actions to be taken
                //

                bool accepted = false;
                while (!accepted)
                {
                    var byIndex = new List<string>();
                    Console.WriteLine("\nActions to be taken:");

                    int nwidth = (int)Math.Ceiling(Math.Log10(changeset.Count(e => e.FileOperation != FileOperation.NoOp)));
                    int n = 0;

                    if (changeset.Count(e => e.FileOperation == FileOperation.UseRepo) > 0)
                    {
                        Console.WriteLine("\nFiles to copy from repository to source:");
                        foreach (var path in changeset.WithFileOperation(FileOperation.UseRepo))
                        {
                            byIndex.Add(path);
                            Console.WriteLine("{0," + nwidth + "}: {1}", ++n, Path.GetFullPath(Path.Combine(repo.Path, path)));
                        }
                    }

                    if (changeset.Count(e => e.FileOperation == FileOperation.UseSource) > 0)
                    {
                        Console.WriteLine("\nFiles to copy from source to repository:");
                        foreach (var path in changeset.WithFileOperation(FileOperation.UseSource))
                        {
                            byIndex.Add(path);
                            Console.WriteLine("{0," + nwidth + "}: {1}", ++n, Path.GetFullPath(Path.Combine(source.Path, path)));
                        }
                    }

                    if (changeset.Count(e => e.FileOperation == FileOperation.DeleteRepo) > 0)
                    {
                        Console.WriteLine("\nFiles to delete from repository:");
                        foreach (var path in changeset.WithFileOperation(FileOperation.DeleteRepo))
                        {
                            byIndex.Add(path);
                            Console.WriteLine("{0," + nwidth + "}: {1}", ++n, Path.GetFullPath(Path.Combine(repo.Path, path)));
                        }
                    }

                    if (changeset.Count(e => e.FileOperation == FileOperation.DeleteSource) > 0)
                    {
                        Console.WriteLine("\nFiles to delete from source:");
                        foreach (var path in changeset.WithFileOperation(FileOperation.DeleteSource))
                        {
                            byIndex.Add(path);
                            Console.WriteLine("{0," + nwidth + "}: {1}", ++n, Path.GetFullPath(Path.Combine(source.Path, path)));
                        }
                    }

                    Console.Write("\nPress Enter to perform actions, or enter a number to edit one: ");
                    string changeNum = Console.ReadLine();

                    if (string.IsNullOrEmpty(changeNum))
                    {
                        accepted = true;
                    }
                    else
                    {
                        try
                        {
                            if (changeNum.Contains("-"))
                            {
                                int n_start, n_end;
                                n_start = int.Parse(changeNum.Substring(0, changeNum.IndexOf('-')).Trim()) - 1;
                                n_end = int.Parse(changeNum.Substring(changeNum.IndexOf('-') + 1).Trim()) - 1;

                                if (n_start < 0 || n_end >= changeset.Count(e => e.FileOperation != FileOperation.NoOp))
                                {
                                    Console.WriteLine("Selection out of range. Try again.");
                                    continue;
                                }

                                FileOperation action = GetActionFromUser("multiple", "multiple");

                                for (int i = n_start; i <= n_end; i++)
                                {
                                    var path = byIndex[i];

                                    string rpath = Path.GetFullPath(Path.Combine(repo.Path, path));
                                    string spath = Path.GetFullPath(Path.Combine(source.Path, path));

                                    if ((action == FileOperation.UseRepo || action == FileOperation.DeleteRepo) && !File.Exists(rpath))
                                    {
                                        Console.WriteLine("Repo path {0} doesn't exist; can't {1} from there. Not changing the action on this one.",
                                            rpath, 
                                            (action == FileOperation.UseRepo) ? "copy" : "delete");
                                    }
                                    else if ((action == FileOperation.UseSource || action == FileOperation.DeleteSource) && !File.Exists(spath))
                                    {
                                        Console.WriteLine("Source path {0} doesn't exist; can't {1} from there. Not changing the action on this one.",
                                            spath,
                                            (action == FileOperation.UseRepo) ? "copy" : "delete");
                                    }
                                    else
                                    {
                                        changeset[path].FileOperation = action;
                                    }
                                }
                            }
                            else
                            {
                                n = int.Parse(changeNum) - 1;
                                if (n < 0 || n >= changeset.Count(e => e.FileOperation != FileOperation.NoOp))
                                    throw new FormatException("out of range");

                                var path = byIndex[n];
                                string rpath = Path.GetFullPath(Path.Combine(repo.Path, path));
                                string spath = Path.GetFullPath(Path.Combine(source.Path, path));

                                FileOperation action = GetActionFromUser(File.Exists(rpath) ? rpath : null, File.Exists(spath) ? spath : null);
                                changeset[path].FileOperation = action;
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
                                throw;
                            }
                        }
                    }
                }

                //
                // Perform the operations
                //

                if (changeset.Count(e => e.FileOperation != FileOperation.NoOp) == 0)
                {
                    Console.WriteLine("Nothing to do.\n");
                    continue;
                }

                var srcFiles = new List<string>();
                var dstFiles = new List<string>();
                var delFiles = new List<string>();

                foreach (var filename in changeset.Where(e => e.FileOperation != FileOperation.NoOp))
                {
                    string repoFile = Path.GetFullPath(Path.Combine(repo.Path, filename));
                    string sourceFile = Path.GetFullPath(Path.Combine(source.Path, filename));

                    switch (changeset[filename].FileOperation)
                    {
                        case FileOperation.UseRepo:
                            srcFiles.Add(repoFile);
                            dstFiles.Add(sourceFile);
                            break;

                        case FileOperation.UseSource:
                            srcFiles.Add(sourceFile);
                            dstFiles.Add(repoFile);
                            break;

                        case FileOperation.DeleteRepo:
                            delFiles.Add(repoFile);
                            break;

                        case FileOperation.DeleteSource:
                            delFiles.Add(sourceFile);
                            break;
                    }
                }

                if (srcFiles.Count > 0)
                {
                    Console.WriteLine("Copying files...");
                    int width = 0;
                    CopyEngine.Copy(srcFiles, dstFiles, delegate(int completed, int total, string file)
                    {
                        string line = string.Format("{0}/{1} {2}", completed, total, file);
                        Console.Write("\r{0,"+width+"}\r", string.Empty);
                        Console.Write(line);
                        width = line.Length;
                    });
                    Console.Write("\r{0," + width + "}\r", string.Empty);
                    Console.WriteLine("Done.");
                }

                if (delFiles.Count > 0)
                {
                    Console.WriteLine("Deleting files...");
                    int width = 0;
                    CopyEngine.Delete(delFiles, delegate(int completed, int total, string file)
                    {
                        string line = string.Format("{0}/{1} {2}", completed, total, file);
                        Console.Write("\r{0," + width + "}\r", string.Empty);
                        Console.Write(line);
                        width = line.Length;
                    });
                    Console.Write("\r{0," + width + "}\r", string.Empty);
                    Console.WriteLine("Done.");
                }

                //TODO: check whether the file copy succeeded and all files were copied

                //
                // Update the repository state
                //

                Console.Write("Updating repository state...");
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

                state.Write(FooSyncEngine.RepoStateFileName);
                Console.WriteLine(" done.\n");
            }
        }

        private FooTree GetFooTree(string path, ICollection<string> exceptions, string type)
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

        private static FileOperation GetActionFromUser(string repo, string source)
        {
            while (true)
            {
                Console.Write("Repository: {0}\nSource: {1}\n", repo ?? "(not present)", source ?? "(not present)");
                Console.Write("Action: ");
                if (repo != null)
                {
                    Console.Write("(1) Copy Repository | ");
                }
                if (source != null)
                {
                    Console.Write("(2) Copy Source | ");
                }
                if (repo != null)
                {
                    Console.Write("(3) Delete Repository | ");
                }
                if (source != null)
                {
                    Console.Write("(4) Delete Source | ");
                }
                Console.Write("(5) Do Nothing : ");

                var key = Console.ReadKey(false);
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        return FileOperation.UseRepo;

                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        return FileOperation.UseSource;

                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        return FileOperation.DeleteRepo;

                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        return FileOperation.DeleteSource;

                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        return FileOperation.NoOp;

                    default:
                        Console.WriteLine("Invalid key pressed.");
                        break;
                }
            }
        }

        private FooSyncEngine Foo { get; set; }
    }
}

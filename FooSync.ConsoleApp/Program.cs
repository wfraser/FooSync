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
            
            Console.WriteLine("FooSync.ConsoleApp v{0} / FooSync v{1}",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                (Type.GetType("FooSync.FooSync FooSync") == null)
                    ? "(unable to load FooSync.dll)" 
                    : System.Reflection.Assembly.GetAssembly(Type.GetType("FooSync.FooSync, FooSync")).GetName().Version.ToString());

            Console.WriteLine("{0} / {1} / {2}",
                Environment.MachineName,
                Environment.OSVersion.Platform,
                Environment.OSVersion.VersionString);
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }
            Console.WriteLine();

            if (programArgs.Flags.Contains("help"))
            {
                Console.WriteLine("usage: {0} [options]", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                Console.WriteLine("Loads its configuration from {0} in the current directory", FooSync.ConfigFileName);
                return;
            }

            var program = new Program(fooOptions);
            program.Run(programArgs);
        }

        Program(Options fooOptions)
        {
            this.Foo = new FooSync(fooOptions);
        }

        void Run(ProgramArguments args)
        {
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

                Console.Write("Enumerating files: repository...");
                FooTree repo = GetFooTree(dir.Path, exceptions, "repository");
                if (repo == null)
                    return;

                Console.Write(" done. Source...");
                
                FooTree source = GetFooTree(dir.Source.Path, exceptions, "source");
                if (source == null)
                    return;

                Console.Write(" done.\n");


                //
                // Load / generate the repository state
                //

                Console.Write("Loading repository state...");
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
                Console.Write(" done.\n");

                //
                // Compute & display the change set
                //

                Console.Write("Computing change set...");
                var changedFiles = Foo.Inspect(repo, source, state);
                Console.Write(" done.\n");

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
                            Debug.Assert(false, "Bogus file change state");
                            break;

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

                        case ChangeStatus.RepoDeleted:
                            descr = "Deleted from repository";
                            break;

                        case ChangeStatus.SourceDeleted:
                            descr = "Deleted from our directory";
                            break;
                    }

                    Console.WriteLine("\t{0}: {1}", descr, file.Key);
                }

                //
                // Check against the repository state
                //

                var conflicts = Foo.GetConflicts(changedFiles, state);
                var fileOperations = new Dictionary<string, FileOperation>();

                foreach (var change in changedFiles)
                {
                    if (change.Value.ConflictStatus == ConflictStatus.NoConflict)
                    {
                        switch (change.Value.ChangeStatus)
                        {
                            case ChangeStatus.Newer:
                            case ChangeStatus.RepoMissing:
                                fileOperations.Add(change.Key, FileOperation.UseSource);
                                break;

                            case ChangeStatus.Older:
                            case ChangeStatus.SourceMissing:
                                fileOperations.Add(change.Key, FileOperation.UseRepo);
                                break;

                            case ChangeStatus.RepoDeleted:
                                fileOperations.Add(change.Key, FileOperation.DeleteSource);
                                break;

                            case ChangeStatus.SourceDeleted:
                                fileOperations.Add(change.Key, FileOperation.DeleteRepo);
                                break;

                            default:
                                Debug.Assert(false, "Invalid change status!");
                                break;
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

                    Console.WriteLine("{0}:\n{1}", descr, conflict.Key);
                    if (conflict.Value.ConflictStatus != ConflictStatus.ChangedInRepoDeletedInSource)
                    {
                        Console.WriteLine("\tSource:     modified {0}, {1} bytes",
                            source.Files[conflict.Key].MTime.ToString(),
                            source.Files[conflict.Key].Size);
                    }
                    if (conflict.Value.ConflictStatus != ConflictStatus.ChangedInSourceDeletedInRepo)
                    {
                        Console.WriteLine("\tRepository: modified {0}, {1} bytes",
                            repo.Files[conflict.Key].MTime.ToString(),
                            repo.Files[conflict.Key].Size);
                    }

                    string rpath = Path.GetFullPath(Path.Combine(repo.Path, conflict.Key));
                    string spath = Path.GetFullPath(Path.Combine(source.Path, conflict.Key));

                    FileOperation? action = GetActionFromUser(File.Exists(rpath) ? rpath : null, File.Exists(spath) ? spath : null);
                    if (action.HasValue)
                    {
                        fileOperations[conflict.Key] = action.Value;
                    }
                }

                //
                // Display overview of actions to be taken
                //

                bool accepted = false;
                while (!accepted)
                {
                    var useRepo = new List<string>();
                    var useSource = new List<string>();
                    var deleteRepo = new List<string>();
                    var deleteSource = new List<string>();

                    Console.WriteLine("\nActions to be taken:");

                    foreach (var change in fileOperations)
                    {
                        switch (change.Value)
                        {
                            case FileOperation.DeleteRepo:
                                deleteRepo.Add(change.Key);
                                break;

                            case FileOperation.DeleteSource:
                                deleteSource.Add(change.Key);
                                break;

                            case FileOperation.UseRepo:
                                useRepo.Add(change.Key);
                                break;

                            case FileOperation.UseSource:
                                useSource.Add(change.Key);
                                break;
                        }
                    }

                    int nwidth = (int)Math.Ceiling(Math.Log10(fileOperations.Count));
                    int n = 1;

                    if (useRepo.Count > 0)
                    {
                        Console.WriteLine("\nFiles to copy from repository to source:");
                        foreach (var path in useRepo)
                        {
                            Console.WriteLine("{0," + nwidth + "}: {1}", n++, Path.GetFullPath(Path.Combine(repo.Path, path)));
                        }
                    }

                    if (useSource.Count > 0)
                    {
                        Console.WriteLine("\nFiles to copy from source to repository:");
                        foreach (var path in useSource)
                        {
                            Console.WriteLine("{0," + nwidth + "}: {1}", n++, Path.GetFullPath(Path.Combine(source.Path, path)));
                        }
                    }

                    if (deleteRepo.Count > 0)
                    {
                        Console.WriteLine("\nFiles to delete from repository:");
                        foreach (var path in deleteRepo)
                        {
                            Console.WriteLine("{0," + nwidth + "}: {1}", n++, Path.GetFullPath(Path.Combine(repo.Path, path)));
                        }
                    }

                    if (deleteSource.Count > 0)
                    {
                        Console.WriteLine("\nFiles to delete from source:");
                        foreach (var path in deleteSource)
                        {
                            Console.WriteLine("{0," + nwidth + "}: {1}", n++, Path.GetFullPath(Path.Combine(source.Path, path)));
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
                            n = int.Parse(changeNum) - 1;
                            if (n < 0 || n >= fileOperations.Count)
                                throw new FormatException("out of range");

                            List<string> listFrom;
                            int listPos;
                            string path;
                            if (n < useRepo.Count)
                            {
                                listFrom = useRepo;
                                listPos = n;
                            }
                            else if (n < useRepo.Count + useSource.Count)
                            {
                                listFrom = useSource;
                                listPos = n - useRepo.Count;
                            }
                            else if (n < useRepo.Count + useSource.Count + deleteRepo.Count)
                            {
                                listFrom = deleteRepo;
                                listPos = n - (useRepo.Count + useSource.Count);
                            }
                            else
                            {
                                listFrom = deleteSource;
                                listPos = n - (useRepo.Count + useSource.Count + deleteRepo.Count);
                            }

                            path = listFrom[listPos];
                            string rpath = Path.GetFullPath(Path.Combine(repo.Path, path));
                            string spath = Path.GetFullPath(Path.Combine(source.Path, path));

                            FileOperation? action = GetActionFromUser(File.Exists(rpath) ? rpath : null, File.Exists(spath) ? spath : null);
                            if (action.HasValue)
                            {
                                fileOperations[path] = action.Value;
                            }
                            else
                            {
                                fileOperations.Remove(path);
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

                if (fileOperations.Count == 0)
                {
                    Console.WriteLine("Nothing to do.");
                    return;
                }

                var srcFiles = new List<string>();
                var dstFiles = new List<string>();
                var delFiles = new List<string>();

                foreach (var c in fileOperations)
                {
                    string repoFile = Path.GetFullPath(Path.Combine(repo.Path, c.Key));
                    string sourceFile = Path.GetFullPath(Path.Combine(source.Path, c.Key));

                    switch (c.Value)
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
                    Console.Write("Copying files...");
                    CopyEngine.Copy(srcFiles, dstFiles);
                    Console.WriteLine(" done.");
                }

                if (delFiles.Count > 0)
                {
                    Console.Write("Deleting files...");
                    CopyEngine.Delete(delFiles);
                    Console.WriteLine(" done.");
                }

                //TODO: check whether the file copy succeeded and all files were copied

                //
                // Update the repository state
                //

                Console.Write("Updating repository state...");
                foreach (var c in changedFiles)
                {
                    string filename = c.Key;
                    ChangeStatus cstatus = c.Value.ChangeStatus;
                    if (fileOperations.ContainsKey(filename))
                    {
                        FileOperation operation = fileOperations[filename];

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
                    }
                }

                foreach (var c in fileOperations)
                {
                    string filename = c.Key;
                    FileOperation operation = c.Value;

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

                state.Write(FooSync.RepoStateFileName);
                Console.Write(" done.");
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

        private static FileOperation? GetActionFromUser(string repo, string source)
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
                        return null;

                    default:
                        Console.WriteLine("Invalid key pressed.");
                        break;
                }
            }
        }

        private FooSync Foo { get; set; }

        private enum FileOperation
        {
            UseRepo,
            UseSource,
            DeleteRepo,
            DeleteSource
        }
    }
}

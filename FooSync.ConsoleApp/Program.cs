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
            Console.WriteLine("{0} / {1} / {2}",
                Environment.MachineName,
                Environment.OSVersion.Platform,
                Environment.OSVersion.VersionString);
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }

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

                FooTree repo = GetFooTree(dir.Path, exceptions, "repository");
                if (repo == null)
                    return;

                FooTree source = GetFooTree(dir.Source.Path, exceptions, "source");
                if (source == null)
                    return;

                var changedFiles = Foo.Inspect(repo, source);

                Console.WriteLine("File changes:");
                foreach (var file in changedFiles)
                {
                    string descr = "ERROR";
                    switch (file.Value.Status)
                    {
                        case FooFileInfo.ChangeStatus.Identical:
                        case FooFileInfo.ChangeStatus.Undetermined:
                            Debug.Assert(false, "Bogus file change state");
                            break;

                        case FooFileInfo.ChangeStatus.Newer:
                            descr = "Newer than the repository";
                            break;

                        case FooFileInfo.ChangeStatus.Older:
                            descr = "Older than the repository";
                            break;

                        case FooFileInfo.ChangeStatus.RepoMissing:
                            descr = "Not in the repository";
                            break;

                        case FooFileInfo.ChangeStatus.SourceMissing:
                            descr = "Not in our directory";
                            break;
                    }

                    Console.WriteLine("\t{0}: {1}", descr, file.Key);
                }
                
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

                if (state.Repository == null)
                {
                    state.AddSource(source, RepositoryState.RepoSourceName);
                }

                if (!state.Sources.ContainsKey(Environment.MachineName.ToLower()))
                {
                    state.AddSource(repo, Environment.MachineName.ToLower());
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

        private FooSync Foo { get; set; }

        enum ChangeStatus
        {
            Identical = 0,
            Older = 1,
            Newer = -1,
            RepoMissing = 2,
            SourceMissing = 3
        }
    }
}

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
            Console.WriteLine(string.Format("{0} / {1}",
                System.Environment.OSVersion.Platform,
                System.Environment.OSVersion.VersionString));
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }

            string repoConfigError;
            RepositoryConfig config = RepositoryConfigLoader.GetRepositoryConfig(FooSync.ConfigFileName, out repoConfigError);

            if (config == null)
            {
                Console.WriteLine("There's a problem with your config file: " + repoConfigError);
                return;
            }

            foreach (var dir in config.Directories)
            {
                var exceptions = FooSync.PrepareExceptions(dir);

                FooTree repo = GetFooTree(dir.Path, exceptions, "repository");
                if (repo == null)
                    return;

                FooTree source = GetFooTree(dir.Source.Path, exceptions, "source");
                if (source == null)
                    return;

                var changedFiles = Foo.Inspect(repo, source);

                foreach (var file in changedFiles)
                {
                    Console.WriteLine(string.Format("{0}: {1}", file.Value.Status, file.Key));
                }
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
                Console.WriteLine(string.Format("{0} directory {1} not found.",
                    type,
                    path));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} reading {1} directory {2}: {3}",
                    ex.GetType().Name,
                    type,
                    path,
                    ex.Message));
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

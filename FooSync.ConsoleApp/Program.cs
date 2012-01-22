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
            //WRFDEV TEST CODE STARTS HERE

            Console.WriteLine(string.Format("Platform: {0}", System.Environment.OSVersion.Platform));
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }
            
            string filename = @"C:\Users\wfraser\FooSync_Repository.xml";

            string repoConfigError;
            RepositoryConfig config = RepositoryConfigLoader.GetRepositoryConfig(filename, out repoConfigError);

            if (config == null)
            {
                Console.WriteLine("There's a problem with your config file: " + repoConfigError);
                return;
            }
            
            System.IO.Directory.SetCurrentDirectory(@"W:\");
            Inspect(config.Directories[0]);

            //WRFDEV TEST CODE ENDS HERE
        }

        void Inspect(RepositoryDirectory dir)
        {
            var exceptions = FooSync.PrepareExceptions(dir);

            FooTree repo, source;
            try
            {
                repo = Foo.Tree(dir.Path, exceptions);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine(string.Format("Repository directory {0} not found.", dir.Path));
                return;
            }

            try
            {
                source = Foo.Tree(dir.Source.Path, exceptions);
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine(string.Format("Source directory {0} not found.", dir.Source.Path));
                return;
            }

            var changedFiles = new Dictionary<string, ChangeStatus>();

            foreach (var file in repo.Files)
            {
                ChangeStatus status = ChangeStatus.Identical;

                if (source.Files.ContainsKey(file.Key))
                {
                    int comp = file.Value.CompareTo(source.Files[file.Key]);
                    if (comp == 0)
                    {
                        file.Value.Identical = true;
                        source.Files[file.Key].Identical = true;
                        continue;
                    }
                    else
                    {
                        status = (ChangeStatus)comp;
                    }
                }
                else
                {
                    status = ChangeStatus.SourceMissing;
                }

                if (status != ChangeStatus.Identical)
                {
                    changedFiles[file.Key] = status;
                }
            }

            foreach (var file in source.Files)
            {
                if (file.Value.Identical)
                {
                    continue;
                }

                Debug.Assert(!repo.Files.ContainsKey(file.Key), "a file slipped through the filter!");

                changedFiles[file.Key] = ChangeStatus.RepoMissing;
            }

            foreach (var file in changedFiles)
            {
                Console.WriteLine(string.Format("{0}: {1}", file.Value, file.Key));
            }
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

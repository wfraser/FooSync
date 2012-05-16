///
/// Codewise/FooSync/NetworkTest/Program.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Codewise.FooSync;

namespace Codewise.FooSync.NetworkTest
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("{0} / {1} / {2}",
                Environment.MachineName,
                Environment.OSVersion.Platform,
                Environment.OSVersion.VersionString);
            if (Type.GetType("Mono.Runtime") != null)
            {
                Console.WriteLine("Using the Mono runtime.");
            }
            Console.WriteLine();

            var hostname = (args.Length > 0) ? args[0] : "127.0.0.1";
            var port     = (args.Length > 1) ? int.Parse(args[1]) : FooSyncUrl.DefaultPort;
            var repo     = (args.Length > 2) ? args[2] : "test";

            var program = new Program();
            program.Run(hostname, port, repo);
        }

        Program()
        {
        }

        void Run(string hostname, int port, string repo)
        {
            var foo = new FooSyncEngine();

            var client = new TcpClient(hostname, port);
            var stream = client.GetStream();
            var writer = new BinaryWriter(stream);
            var reader = new BinaryReader(stream);

            int ret = 0;
            int count = 0;

            var password = new System.Security.SecureString();
            foreach (char c in "qwerty")
                password.AppendChar(c);

            writer.Write(OpCode.Auth);
            writer.Write("anonymous");
            writer.Write(password);
            ret = reader.ReadInt32();

            Console.WriteLine("auth returned {0}", ret);

            writer.Write(OpCode.ListRepos);
            ret = reader.ReadInt32();
            count = reader.ReadInt32();

            Console.WriteLine("listrepos returned {0}", ret);
            Console.WriteLine("{0} repositories", count);

            for (int x = 0; x < count; x++)
            {
                var repoName = reader.ReadString();
                Console.WriteLine("> {0}", repoName);
            }

            writer.Write(OpCode.Hello);
            var s = reader.ReadString();

            Console.WriteLine("hello replied {0}", s);

            writer.Write(OpCode.Tree);
            writer.Write("test");
            ret = reader.ReadInt32();

            Console.WriteLine("tree returned {0}", ret);

            var tree = new FooTree(foo, string.Format("fs://{0}:{1}/{2}", hostname, port, repo), stream,
                (item, total, path) =>
                    {
                        Console.WriteLine("{0}: {1}", item, path);
                    }
                );

            Console.WriteLine("{0} items in tree", tree.Files.Count);

            stream.Close();
        }
    }
}

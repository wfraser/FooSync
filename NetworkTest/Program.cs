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
            var port     = (args.Length > 1) ? int.Parse(args[1]) : 22022;
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

            NetUtil.WriteInt(stream, (int)OpCode.ListRepos);
            var i = NetUtil.GetInt(stream);
            var count = NetUtil.GetInt(stream);
            for (int x = 0; x < count; x++)
            {
                var repoName = NetUtil.GetString(stream);
            }

            NetUtil.WriteInt(stream, (int)OpCode.Auth);
            NetUtil.WriteString(stream, repo);
            i = NetUtil.GetInt(stream);

            Console.WriteLine("auth returned {0}", i);

            NetUtil.WriteInt(stream, (int)OpCode.Hello);
            var s = NetUtil.GetString(stream);

            Console.WriteLine("hello replied {0}", s);

            NetUtil.WriteInt(stream, (int)OpCode.Tree);
            i = NetUtil.GetInt(stream);

            Console.WriteLine("tree returned {0}", i);

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

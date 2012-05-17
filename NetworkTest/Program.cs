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
using System.Linq;
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

            var passwd = new System.Security.SecureString();
            foreach (char c in "qwerty")
                passwd.AppendChar(c);

            RetCode ret;
            int count = 0;

            writer.Write(OpCode.Auth);
            writer.Write("test");
            writer.Write(passwd);
            ret = reader.ReadRetCode();

            Console.WriteLine("auth returned {0}", ret);

            writer.Write(OpCode.ListRepos);
            ret = reader.ReadRetCode();
            count = reader.ReadInt32();

            Console.WriteLine("listrepos returned {0}", ret);
            Console.WriteLine("{0} repositories", count);

            for (int x = 0; x < count; x++)
            {
                var repoName = reader.ReadString();
                Console.WriteLine("> {0}", repoName);
            }

            writer.Write(OpCode.Hello);
            ret = reader.ReadRetCode();
            var s = reader.ReadString();

            Console.WriteLine("hello replied {0}", s);

            writer.Write(OpCode.Tree);
            writer.Write("test");
            ret = reader.ReadRetCode();

            Console.WriteLine("tree returned {0}", ret);

            var tree = new FooTree(foo, string.Format("fs://{0}:{1}/{2}", hostname, port, repo), stream,
                (item, total, path) =>
                    {
                        Console.WriteLine("{0}: {1}", item, path);
                    }
                );

            Console.WriteLine("{0} items in tree", tree.Files.Count);

            writer.Write(OpCode.GetFile);
            writer.Write("test");
            writer.Write(tree.Files.Keys.First());
            ret = reader.ReadRetCode();

            Console.WriteLine("getfile returned {0}", ret);

            var len = reader.ReadInt64();
            var bytes = reader.ReadBytes((int)len);

            stream.Close();
        }
    }
}

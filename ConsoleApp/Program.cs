﻿///
/// Codewise/FooSync/ConsoleApp/Program.cs
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
using System.Text.RegularExpressions;
using Codewise.ArgumentParser;
using Codewise.FooSync;

namespace Codewise.FooSync.ConsoleApp
{
    class Program
    {
        private static readonly string[] MODES = { "create", "add", "rm", "sync" };

        [STAThread]
        static void Main(string[] args)
        {
            var programArgs = new ProgramArguments(args);
            var fooOptions = new Options();

            if (programArgs.Flags.ContainsKey("casesensitive"))
            {
                fooOptions.CaseInsensitive = !programArgs.Flags["casesensitive"];
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
                Console.WriteLine("usage: {0} [options]", ProgramName);
                Console.WriteLine("Loads its configuration from {0} in the current directory", FooSyncEngine.ConfigFileName);
                return;
            }

            var program = new Program(foo);
            program.Run(programArgs);
        }

        static string ProgramName
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; }
        }

        Program(FooSyncEngine foo)
        {
            this.Foo = foo;
        }

        void Run(ProgramArguments args)
        {
            string mode = CommandLineGetMode(args, "sync");

            switch (mode)
            {
                case null:
                    Console.WriteLine("usage: {0} <command>", ProgramName);
                    Console.WriteLine("where <command> is one of: {0}", string.Join(", ", MODES));
                    break;

                case "create":
                    {
                        if (args.Ordinals.Count != 2)
                        {
                            Console.WriteLine("usage: {0} --create <sync group name> <filename>.fsg", ProgramName);
                            return;
                        }

                        CreateSyncGroup(args.Ordinals[0], args.Ordinals[1]);
                    }
                    break;

                case "add":
                    {
                        if (args.Ordinals.Count != 2)
                        {
                            Console.WriteLine("usage: {0} --add <filename>.fsg <url/path>", ProgramName);
                            return;
                        }

                        SyncGroupConfig syncGroup = LoadSyncGroupConfig(args.Ordinals[0]);
                        SyncGroupConfigMember newMember = new SyncGroupConfigMember();

                        FooSyncUrl newUrl = NormalizePath(args.Ordinals[1]);
                        if (newUrl == null)
                        {
                            Console.WriteLine("Invalid path or URL \"{0}\"", args.Ordinals[1]);
                            return;
                        }

                        newMember.URL = newUrl.ToString();
                        if (newUrl.IsLocal)
                        {
                            newMember.Host = Environment.MachineName;
                        }

                        if (!newMember.URL.Equals(args.Ordinals[1]))
                        {
                            Console.WriteLine("Path or URL {0} interpreted as {1}", args.Ordinals[1], newMember.URL);
                        }

                        foreach (SyncGroupConfigMember existingMember in syncGroup.Members)
                        {
                            try
                            {
                                FooSyncUrl url = new FooSyncUrl(existingMember.URL);

                                if (url.Equals(newUrl, Foo.Options.CaseInsensitive)
                                        && existingMember.Host.Equals(newMember.Host, StringComparison.OrdinalIgnoreCase)
                                        && !string.IsNullOrEmpty(newMember.Host))
                                {
                                    Console.WriteLine("{0} is already a member of that sync group. Aborting.",
                                        newMember.URL);
                                    return;
                                }
                            }
                            catch (FormatException)
                            {
                            }
                        }

                        if (args.Options.ContainsKey("user"))
                        {
                            newMember.Auth = new SyncGroupConfigMemberAuth();
                            newMember.Auth.User = args.Options["user"];

                            if (args.Options.ContainsKey("pass"))
                            {
                                newMember.Auth.Password = args.Options["pass"];
                            }

                            //TODO: option for prompting at console
                        }

                        syncGroup.Members.Add(newMember);
                        XmlConfigLoader.Write(syncGroup, args.Ordinals[0]);

                        Console.WriteLine("Added {0} to sync group \"{1}\"",newMember.URL, syncGroup.Name);
                    }
                    break;

                case "rm":
                    {
                        if (args.Ordinals.Count != 2)
                        {
                            Console.WriteLine("usage: {0} --rm <filename>.fsg <url/path>", ProgramName);
                            return;
                        }

                        SyncGroupConfig syncGroup = LoadSyncGroupConfig(args.Ordinals[0]);

                        FooSyncUrl rmUrl = NormalizePath(args.Ordinals[1]);
                        if (rmUrl == null)
                        {
                            Console.WriteLine("Invalid path or URL \"{0}\"", args.Ordinals[1]);
                            return;
                        }

                        if (!args.Ordinals[1].Equals(rmUrl.ToString()))
                        {
                            Console.WriteLine("Path or URL {0} interpreted as {1}", args.Ordinals[1], rmUrl.ToString());
                        }

                        SyncGroupConfigMember rmMember = null;
                        foreach (SyncGroupConfigMember member in syncGroup.Members)
                        {
                            FooSyncUrl memUrl;
                            try
                            {
                                memUrl = new FooSyncUrl(member.URL);
                            }
                            catch (FormatException)
                            {
                                continue;
                            }

                            if (memUrl.Equals(rmUrl, Foo.Options.CaseInsensitive)
                                && !string.IsNullOrEmpty(member.Host)
                                && member.Host.Equals(Environment.MachineName))
                            {
                                rmMember = member;
                                break;
                            }
                        }

                        if (rmMember != null)
                        {
                            syncGroup.Members.Remove(rmMember);
                        }
                        else
                        {
                            Console.WriteLine("Sync group member {0} not found.", rmUrl.ToString());
                            return;
                        }

                        XmlConfigLoader.Write(syncGroup, args.Ordinals[0]);

                        Console.WriteLine("Removed {0} from sync group \"{1}\"", rmMember.URL, syncGroup.Name);
                    }
                    break;

                case "sync":
                    {
                        if (args.Ordinals.Count != 1)
                        {
                            Console.WriteLine("usage: {0} --sync <filename>.fsg", ProgramName);
                            return;
                        }

                        SyncGroupConfig syncGroup = LoadSyncGroupConfig(args.Ordinals[0]);

                        Console.WriteLine("Syncing sync group \"{0}\" with {1} members.",
                            syncGroup.Name,
                            syncGroup.Members.Count);

                        Sync(syncGroup);

                        Console.WriteLine("Done syncing \"{0}\".", syncGroup.Name);
                    }
                    break;
            }
        }

        string CommandLineGetMode(ProgramArguments args, string defaultMode)
        {
            string mode = null;
            IQueryable<string> modes = MODES.AsQueryable<string>();

            foreach (string flag in args.Flags.Keys)
            {
                if (modes.Contains(flag))
                {
                    if (mode != null)
                    {
                        return null;
                    }

                    mode = flag;
                }
            }

            return mode ?? defaultMode;
        }

        FooSyncUrl NormalizePath(string given)
        {
            if (!given.StartsWith("fs://"))
            {
                given = Path.GetFullPath(given);
            }

            if (given.StartsWith("/"))
            {
                given = "file://" + given;
            }

            FooSyncUrl url;
            try
            {
                url = new FooSyncUrl(given);
            }
            catch (FormatException)
            {
                return null;
            }

            return url;
        }

        void CreateSyncGroup(string name, string filename)
        {
            SyncGroupConfig config = new SyncGroupConfig();
            config.Version = 1;
            config.Name = name;
            config.IgnoreGlob = new IgnorePatterns();
            config.IgnorePatterns = new IgnorePatterns();

            XmlConfigLoader.Write(config, filename);
        }

        SyncGroupConfig LoadSyncGroupConfig(string xmlFilename)
        {
            string failMsg;
            SyncGroupConfig syncGroup = LoadSyncGroupConfig(xmlFilename, out failMsg);
            
            if (syncGroup == null)
            {
                Console.WriteLine("There's a problem with your sync group config file:\n" + failMsg);
                Environment.Exit(-1);
            }

            return syncGroup;
        }

        SyncGroupConfig LoadSyncGroupConfig(string xmlFilename, out string failMsg)
        {
            SyncGroupConfig config;

            if (!XmlConfigLoader.Validate(
                    xmlFilename,
                    Path.Combine(
                        Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                        "SyncGroupConfig.xsd"),
                    out failMsg))
            {
                return null;
            }

            try
            {
                config = XmlConfigLoader.Load<SyncGroupConfig>(xmlFilename);
            }
            catch (Exception ex)
            {
                failMsg = XmlConfigLoader.FormatExceptionMessage(ex);
                return null;
            }

            foreach (SyncGroupConfigMember member in config.Members)
            {
                if (member.URL.StartsWith("file:///") && string.IsNullOrEmpty(member.Host))
                {
                    failMsg = "file:// URL given but not Host: " + member.URL;
                    return null;
                }
            }

            failMsg = string.Empty;
            return config;
        }

        void Sync(SyncGroupConfig syncGroup)
        {
            throw new NotImplementedException("Sync action unimplemented");
        }

        private FooTree GetFooTree(string path, ICollection<string> exceptions, string type)
        {
            FooTree ret = null;
            try
            {
                ret = new FooTree(Foo, path, exceptions);
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

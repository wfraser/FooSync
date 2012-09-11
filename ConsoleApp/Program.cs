///
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
                //TODO
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

                        newMember.UrlString = newUrl.ToString();
                        if (newUrl.IsLocal)
                        {
                            newMember.Host = Environment.MachineName;
                        }

                        if (!newMember.UrlString.Equals(args.Ordinals[1]))
                        {
                            Console.WriteLine("Path or URL {0} interpreted as {1}", args.Ordinals[1], newMember.UrlString);
                        }

                        foreach (SyncGroupConfigMember existingMember in syncGroup.Members)
                        {
                            try
                            {
                                if (existingMember.Url.Equals(newUrl, Foo.Options.CaseInsensitive)
                                        && existingMember.Host.Equals(newMember.Host, StringComparison.OrdinalIgnoreCase)
                                        && !string.IsNullOrEmpty(newMember.Host))
                                {
                                    Console.WriteLine("{0} is already a member of that sync group. Aborting.",
                                        newMember.UrlString);
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

                        Console.WriteLine("Added {0} to sync group \"{1}\"",newMember.UrlString, syncGroup.Name);
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
                            if (member.Url.Equals(rmUrl, Foo.Options.CaseInsensitive)
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

                        Console.WriteLine("Removed {0} from sync group \"{1}\"", rmMember.UrlString, syncGroup.Name);
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
            config.IgnorePatterns = new IgnorePattern[] { };

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
                if (member.UrlString.StartsWith("file:///") && string.IsNullOrEmpty(member.Host))
                {
                    failMsg = "file:// URL given but not Host: " + member.UrlString;
                    return null;
                }
            }

            failMsg = string.Empty;
            return config;
        }

        void Sync(SyncGroupConfig syncGroup)
        {
            //throw new NotImplementedException("Sync action unimplemented");

            if (syncGroup.Members.Count == 0)
            {
                Console.WriteLine("Sync group has no members; nothing to sync! Aborting.");
                return;
            }

            if (syncGroup.Members.Count == 1)
            {
                Console.WriteLine("Sync group has only one member; nothing to sync! Aborting.");
                return;
            }
            
            for (int i = 0; i < syncGroup.Members.Count; i++)
            {
                SyncGroupConfigMember member = syncGroup.Members[i];

                if (member.Host != null
                        && !Environment.MachineName.Equals(member.Host, StringComparison.OrdinalIgnoreCase))
                {
                    syncGroup.Members.RemoveAt(i);
                    i--;
                }
            }

            Dictionary<SyncGroupConfigMember, FooTree> trees = GetTrees(syncGroup);
            Dictionary<SyncGroupConfigMember, RepositoryStateCollection> states = GetStates(syncGroup, trees);
        }

        private Dictionary<SyncGroupConfigMember, FooTree> GetTrees(SyncGroupConfig syncGroup)
        {
            Dictionary<SyncGroupConfigMember, FooTree> trees = new Dictionary<SyncGroupConfigMember, FooTree>();
            ICollection<string> exceptions = FooSyncEngine.PrepareExceptions(syncGroup.IgnorePatterns);
            Progress enumCallback = new Progress((current, total, name) =>
            {
                Console.Write("\r{0} items...", current);
            });

            foreach (SyncGroupConfigMember member in syncGroup.Members)
            {
                Console.Write("Enumerating files in ");

                FooTree tree = null;
                if (member.Url.IsLocal)
                {
                    Console.WriteLine(member.Url.LocalPath);

                    try
                    {
                        tree = new FooTree(Foo, member.Url.LocalPath, exceptions, enumCallback);
                        Console.WriteLine("\r{0} items.  ", tree.Files.Count);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.WriteLine("Directory {1} not found.", member.Url.LocalPath);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0} reading directory {1}: {2}",
                            ex.GetType().Name,
                            member.Url.LocalPath,
                            ex.Message);
                        continue;
                    }
                }
                else
                {
                    Console.WriteLine(member.UrlString);

                    NetClient nc = new NetClient(
                        Foo,
                        member.Url.Host,
                        member.Url.Port,
                        member.Auth == null ? string.Empty : member.Auth.User,
                        member.Auth == null ? string.Empty : member.Auth.Password,
                        member.Url.AbsolutePath.Substring(1)
                        );

                    tree = nc.GetTree(enumCallback);
                    Console.WriteLine("\r{0} items.  ", tree.Files.Count);
                }

                trees.Add(member, tree);
            }

            return trees;
        }

        private Dictionary<SyncGroupConfigMember, RepositoryStateCollection> GetStates(SyncGroupConfig syncGroup, Dictionary<SyncGroupConfigMember, FooTree> trees)
        {
            Dictionary<SyncGroupConfigMember, RepositoryStateCollection> states = new Dictionary<SyncGroupConfigMember, RepositoryStateCollection>();

            foreach (SyncGroupConfigMember member in syncGroup.Members)
            {
                RepositoryStateCollection state = null;

                if (member.Url.IsLocal)
                {
                    try
                    {
                        state = new RepositoryStateCollection(Path.Combine(member.Url.LocalPath, FooSyncEngine.RepoStateFileName));
                    }
                    catch (FileNotFoundException)
                    {
                        state = new RepositoryStateCollection();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Unexpected {0} trying to read repository state: {1}",
                            ex.GetType().Name,
                            ex.Message);
                        continue;
                    }
                }
                else
                {
                    NetClient nc = new NetClient(
                        Foo,
                        member.Url.Host,
                        member.Url.Port,
                        member.Auth == null ? string.Empty : member.Auth.User,
                        member.Auth == null ? string.Empty : member.Auth.Password,
                        member.Url.AbsolutePath.Substring(1)
                        );

                    state = nc.GetState();
                }

                if (state.Repository == null)
                {
                    state.AddRepository(trees[member], state.RepositoryID);
                }

                foreach (var otherPair in states)
                {
                    SyncGroupConfigMember otherMember = otherPair.Key;
                    RepositoryStateCollection otherState = otherPair.Value;

                    if (!state.Repositories.ContainsKey(otherState.RepositoryID))
                    {
                        state.AddRepository(trees[otherMember], otherState.RepositoryID);
                    }

                    if (!otherState.Repositories.ContainsKey(state.RepositoryID))
                    {
                        otherState.AddRepository(trees[member], state.RepositoryID);
                    }
                }

                states.Add(member, state);
            }

            foreach (var pair in states)
            {
                SyncGroupConfigMember member = pair.Key;
                RepositoryStateCollection state = pair.Value;

                if (member.Url.IsLocal)
                {
                    state.Write(Path.Combine(member.Url.LocalPath, FooSyncEngine.RepoStateFileName));
                }
                else
                {
                    //TODO
                }
            }

            return states;
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
                    Console.Write("(1) Take Other Version | ");
                }
                if (source != null)
                {
                    Console.Write("(2) Copy This Version | ");
                }
                if (repo != null)
                {
                    Console.Write("(3) Delete | ");
                }
                Console.Write("(4) Do Nothing : ");

                var key = Console.ReadKey(false);
                Console.WriteLine();
                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        return FileOperation.Take;

                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        return FileOperation.Give;

                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        return FileOperation.Delete;

                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
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

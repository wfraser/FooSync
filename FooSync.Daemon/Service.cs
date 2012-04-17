///
/// Codewise/FooSync/Daemon/Service.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Codewise.ArgumentParser;
using Codewise.FooSync;

namespace Codewise.FooSync.Daemon
{
    [System.ComponentModel.DesignerCategory("Code")] // suppress annoying VS behavior
    public class FooSyncService : ServiceBase
    {
        public static readonly int DefaultPort = 22022;
        public static readonly string Name = "FooSyncService";
        public static readonly string DisplayName = "FooSync Daemon";
        public static readonly string Description = "Serves FooSync repositories across the network.";

        private static readonly string DefaultConfig = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "foosyncd_config.xml");

        public FooSyncService()
        {
            ServiceName = Name;
            _shuttingDown = false;
        }

        internal void Start(string[] args)
        {
            var programArgs = new ProgramArguments(args);

            if (programArgs.Flags.ContainsKey("help"))
            {
                Console.WriteLine("usage: {0} [options]", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                Console.WriteLine("TODO: documentation");
                return;
            }

            ProcessArguments(programArgs);

            Run();
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif
            base.OnStart(args);
            Start(args);
        }

        protected override void OnStop()
        {
            _shuttingDown = true;

#if !__MonoCS__
            _listen4.Stop();
            _v4thread.Join(10 * 1000);
#endif

            _listen6.Stop();
            _v6thread.Join(10 * 1000);

            base.OnStop();
        }

        private void ProcessArguments(ProgramArguments args)
        {
            _args = args;

            var fooOptions = new Options();

            if (_args.Flags.ContainsKey("casesensitive"))
            {
                fooOptions.CaseInsensitive = _args.Flags["casesensitive"];
            }
            else
            {
                //
                // default to case-sensitive on Unix
                //
                fooOptions.CaseInsensitive = !(Environment.OSVersion.Platform == PlatformID.Unix);
            }

            _listenPort = DefaultPort;
            if (_args.Options.ContainsKey("port"))
            {
                try
                {
                    _listenPort = int.Parse(_args.Options["port"]);
                }
                catch (FormatException)
                {
                    // keep default port
                }
            }

            var configPath = DefaultConfig;
            if (_args.Options.ContainsKey("config"))
            {
                configPath = _args.Options["config"];
            }

            try
            {
                string err = string.Empty;
                _config = ServerRepositoryConfigLoader.GetConfig(configPath, out err);

                if (_config == null)
                {
                    Trace.TraceError("Error loading FooSync daemon config: {0}", err);
                    throw new Exception(string.Format("Error loading FooSync daemon config: {0}", err));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error loading FooSync daemon config: {0}: {1}{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
                throw;
            }

            _exceptions = new Dictionary<string, ICollection<string>>();

            foreach (var repo in _config.Repositories)
            {
                _exceptions.Add(repo.Key, FooSyncEngine.PrepareExceptions(repo.Value.IgnoreRegex, repo.Value.IgnoreGlob));
            }

            _foo = new FooSyncEngine(fooOptions);
        }

        private void Run()
        {
#if !__MonoCS__
            _listen4 = new TcpListener(IPAddress.Any, _listenPort);
            _v4thread = new Thread(Listener_AcceptClient);
            _v4thread.Start(_listen4);
#endif

            _listen6 = new TcpListener(IPAddress.IPv6Any, _listenPort);
            _v6thread = new Thread(Listener_AcceptClient);
            _v6thread.Start(_listen6);
        }

        void Listener_AcceptClient(object arg)
        {
            var listener = (TcpListener)arg;
            listener.Start();
            while (!_shuttingDown)
            {
                try
                {
                    var client = listener.AcceptTcpClient();
                    var session = new Session(client, _foo, _config, _exceptions);
                    var thread = new Thread(session.Run);
                    thread.Name = "client " + ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
                    thread.Start();
                }
                catch (SocketException ex)
                {
                    if (!_shuttingDown || ex.ErrorCode != 10004) // this gets thrown when OnStop Stop()s the listeners.
                    {
                        Trace.TraceError("SocketException Code {2}: {0}\r\n{1}", ex.Message, ex.StackTrace, ex.ErrorCode);
                    }
                }
            }
        }

        private ProgramArguments _args;
        private int              _listenPort;
        private FooSyncEngine    _foo;
        private ServerRepositoryConfig _config;
        private Dictionary<string, ICollection<string>> _exceptions;

        //
        // In Linux, it seems an IPv6 socket can accept IPv4 connections.
        // In fact, if we attempt to bind both IPv4 and IPv6 sockets,
        //   one will fail due to the address being in use.
        //
#if !__MonoCS__
        private Thread           _v4thread;
        private TcpListener      _listen4;
#endif
        
        private Thread           _v6thread;
        private TcpListener      _listen6;
        private bool             _shuttingDown;
    }
}

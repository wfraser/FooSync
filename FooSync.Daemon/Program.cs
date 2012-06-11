///
/// Codewise/FooSync/Daemon/Program.cs
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
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Codewise.FooSync.Daemon
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Type.GetType("Mono.Runtime") != null // mono-service2 sucks; run as foreground process instead
#if DEBUG
                || Debugger.IsAttached
#endif
                )
            {
                try
                {
                    var svc = new FooSyncService();
                    var args = Environment.GetCommandLineArgs();
                    svc.Start(args.Where((arg, index) => index > 0).ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unhandled {0}: {1}", ex.GetType().Name, ex.Message);
		    Console.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new FooSyncService() 
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}

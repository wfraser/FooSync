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
#if DEBUG
            if (Debugger.IsAttached)
            {
#endif //DEBUG

//
// mono-service2 is really buggy and unreliable.
// Run as an ordinary foreground process instead.
//
#if DEBUG || __MonoCS__
                var svc = new FooSyncService();
                var args = Environment.GetCommandLineArgs();
                svc.Start(args.Where((arg, index) => index > 0).ToArray());
#endif //DEBUG or MonoCS

#if DEBUG
            }
            else
#endif //DEBUG
#if !__MonoCS__
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
                { 
                    new FooSyncService() 
                };
                ServiceBase.Run(ServicesToRun);
            }
#else
            { }
#endif // !MonoCS
        }
    }
}

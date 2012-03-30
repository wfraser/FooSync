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
                var svc = new FooSyncService();
                svc.Start(new string[]{ });
            }
            else
#endif //DEBUG
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

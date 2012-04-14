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
                svc.Start(new string[]{
                    "/config",
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Personal), // "My Documents"
                        "foosyncd_config.xml"
                    )
                });

                //
                // test code
                //

                /*
                
                var client = new System.Net.Sockets.TcpClient("127.0.0.1", FooSyncService.DefaultPort);
                var stream = client.GetStream();

                NetUtil.WriteInt(stream, (int)Session.OpCode.Auth);
                NetUtil.WriteString(stream, "test");
                var i = NetUtil.GetInt(stream);

                NetUtil.WriteInt(stream, (int)Session.OpCode.Hello);
                var s = NetUtil.GetString(stream);

                NetUtil.WriteInt(stream, (int)Session.OpCode.Tree);
                i = NetUtil.GetInt(stream);

                if (i == (int)Session.RetCode.Success)
                {
                    var tree = FooTree.Unserialize(stream);
                }

                NetUtil.WriteInt(stream, (int)Session.OpCode.State);
                i = NetUtil.GetInt(stream);

                if (i == (int)Session.RetCode.Success)
                {
                    var state = new RepositoryState();

                    var stateLength = NetUtil.GetLong(stream);
                    var tempBuf = new byte[stateLength];
                    var bufPos = 0;

                    while (bufPos < stateLength)
                    {
                        bufPos += stream.Read(tempBuf, bufPos, (int)stateLength - bufPos);
                    }

                    state.Read(new System.IO.StreamReader(new System.IO.MemoryStream(tempBuf, false)));
                }

                NetUtil.WriteInt(stream, (int)Session.OpCode.File);
                NetUtil.WriteString(stream, "test.txt");
                i = NetUtil.GetInt(stream);

                if (i == (int)Session.RetCode.Success)
                {
                    var len = NetUtil.GetLong(stream);
                    var buf = new byte[len];
                    int bufPos = 0;

                    while (bufPos < len)
                    {
                        bufPos += stream.Read(buf, bufPos, (int)len - bufPos);
                    }

                    var file = Encoding.UTF8.GetString(buf);
                }

                stream.Close();
                 */
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

///
/// Codewise/FooSync/Daemon/Session.cs
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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Text;

namespace Codewise.FooSync.Daemon
{
    public class Session
    {
        public bool UseSSL { get; set; }

        private TcpClient _client;
        private SslStream _ssl;
        private Stream    _stream;
        private FooSyncEngine _foo;

        public Session(TcpClient client, FooSyncEngine foo)
        {
            UseSSL = false; // default to false until this functionality actually works
            _client = client;
            _foo = foo;
        }

        /// <summary>
        /// Handle the session.
        /// Use this as the main method in session threads.
        /// </summary>
        public void Run()
        {
            if (UseSSL)
            {
                _ssl = new SslStream(_client.GetStream(), true);
                _stream = _ssl;
            }
            else
            {
                _stream = _client.GetStream();
            }

            bool authenticated = false;

            try
            {
                while (_client.Connected)
                {
                    //
                    // Handle any number of requests in the connection, as long as an auth request
                    // comes first and succeeds.
                    //

                    var opCode = (OpCode)NetUtil.GetInt(_stream);

                    if (!authenticated && opCode != OpCode.Auth)
                    {
                        NetUtil.WriteInt(_stream, (int)RetCode.BadAuth);
                        _client.Close();
                        return;
                    }

                    switch ((OpCode)opCode)
                    {
                        case OpCode.Auth:
                            authenticated = HandleAuthRequest();
                            break;

                        case OpCode.Tree:
                            HandleTreeRequest();
                            break;

                        case OpCode.State:
                            HandleStateRequest();
                            break;

                        case OpCode.File:
                            HandleFileRequest();
                            break;

                        default:
                            NetUtil.WriteInt(_stream, (int)RetCode.BadOp);
                            _client.Close();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                UnhandledException(ex);
                _client.Close();
            }
        }

        private void UnhandledException(Exception ex)
        {
            Trace.TraceError("Unhandled {0}: {1}{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            if (ex.InnerException != null)
            {
                Trace.TraceError("Inner {0}: {1}{2}", ex.GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        /// <summary>
        /// Handle an authentication request. This must be the first request of a connection.
        /// 
        /// Currently just says hello, and returns success.
        /// </summary>
        /// <returns>true if the user is authenticated; false otherwise</returns>
        private bool HandleAuthRequest()
        {
            // TODO
            
            NetUtil.WriteInt(_stream, (int)RetCode.Success);

            NetUtil.WriteString(
                _stream,
                "Codewise.FooSync.Daemon says hello "
                    + (_client.Client.RemoteEndPoint as IPEndPoint).Address.ToString()
            );

            return true;
        }

        /// <summary>
        /// Handle a tree request.
        /// 
        /// Reads a path from the stream, then writes a return code, followed by the serialized
        /// tree (the number of files, followed by each file path, source, file MTime (in ticks),
        /// and size).
        /// </summary>
        private void HandleTreeRequest()
        {
            var path = NetUtil.GetString(_stream);

            try
            {
                var tree = _foo.Tree(path);

                NetUtil.WriteInt(_stream, (int)RetCode.Success);
                tree.Serialize(_stream);
            }
            catch (FileNotFoundException)
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadPath);
            }
        }

        /// <summary>
        /// Handle a Repository State request
        /// 
        /// Gets a path from the stream, then writes a return code, the size of the state data,
        /// and then invokes RepositoryState.Write (which writes the same way it does to a file).
        /// </summary>
        private void HandleStateRequest()
        {
            var path = NetUtil.GetString(_stream);

            try
            {
                var state = new RepositoryState(Path.Combine(path, FooSyncEngine.RepoStateFileName));

                NetUtil.WriteInt(_stream, (int)RetCode.Success);

                using (var statebuf = new MemoryStream())
                {
                    state.Write(new StreamWriter(statebuf));

                    NetUtil.WriteLong(_stream, statebuf.Length);

                    _stream.Write(statebuf.GetBuffer(), 0, (int)(statebuf.Length));
                }
            }
            catch (FileNotFoundException)
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadPath);
            }
        }

        /// <summary>
        /// Handle a file request.
        /// 
        /// Gets a path from the stream, then writes a return code, the byte count of the file,
        /// followed by that many bytes of file data.
        /// </summary>
        private void HandleFileRequest()
        {
            var path = NetUtil.GetString(_stream);

            try
            {
                using (var file = new FileStream(path, FileMode.Open))
                {
                    NetUtil.WriteInt(_stream, (int)RetCode.Success);
                    NetUtil.WriteLong(_stream, file.Length);

                    byte[] buf = new byte[4096];
                    while (file.CanRead)
                    {
                        var nread = file.Read(buf, 0, buf.Length);
                        _stream.Write(buf, 0, nread);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadPath);
            }
        }

        enum OpCode : int
        {
            Auth,
            Tree,
            State,
            File
        }

        enum RetCode : int
        {
            Success,
            UnknownError,
            BadAuth,
            BadPath,
            BadOp
        }
    }
}

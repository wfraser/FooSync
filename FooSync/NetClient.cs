///
/// Codewise/FooSync/NetClient.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Codewise.FooSync
{
    public class NetClient
    {
        private FooSyncEngine _foo;
        private string    _hostname;
        private int       _port;
        private string    _repoName;
        private TcpClient _client;
        private Stream    _stream;

        public NetClient(FooSyncEngine foo, string hostname, int port, string repoName)
        {
            _foo      = foo;
            _hostname = hostname;
            _port     = port;
            _repoName = repoName;
            _client   = null;
            _stream   = null;
        }

        public static ICollection<string> ListRepositories(string hostname, int port, string repoName)
        {
            var list = new List<string>();

            return list;
        }

        public static bool IsDisconnect(Exception ex)
        {
            if (ex is IOException)
            {
                var se = ex.InnerException as SocketException;
                if (se != null && se.ErrorCode == 10053) // remote endpoint disconnected
                {
                    return true;
                }
            }

            return false;
        }

        private void Auth()
        {
            if (_client == null || _client.Connected)
                return;

            _client = new TcpClient(_hostname, _port);
            _stream = _client.GetStream();

            NetUtil.WriteInt(_stream, (int)OpCode.Auth);
            NetUtil.WriteString(_stream, _repoName);

            int i = NetUtil.GetInt(_stream);

            if (i != (int)RetCode.Success)
                throw new AuthException(string.Format("Authentication with FooSync server failed: code {0}", ((RetCode)i).ToString()));
        }

        public FooTree GetTree(Progress callback = null)
        {
            while (true)
            {
                try
                {
                    NetUtil.WriteInt(_stream, (int)OpCode.Tree);
                    int i = NetUtil.GetInt(_stream);

                    if (i != (int)RetCode.Success)
                        throw new FooNetException(string.Format("Failed to get tree from FooSync server: code {0}", ((RetCode)i).ToString()));

                    return new FooTree(_foo, string.Format("fs://{0}:{1}/{2}", _hostname, _port, _repoName), _stream, callback);
                }
                catch (AuthException)
                {
                    throw;
                }
                catch (FooNetException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (IsDisconnect(ex))
                    {
                        Auth();
                    }

                    throw;
                }
            }
        }

        private class AuthException : Exception
        {
            public AuthException() : base() { }
            public AuthException(string message) : base(message) { }
        }

        private class FooNetException : Exception
        {
            public FooNetException() : base() { }
            public FooNetException(string message) : base(message) { }
        }
    }
}

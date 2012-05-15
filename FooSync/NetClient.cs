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
using System.Security;
using System.Text;

namespace Codewise.FooSync
{
    public class NetClient
    {
        private FooSyncEngine _foo;
        private string        _hostname;
        private int           _port;
        private string        _repoName;
        private TcpClient     _client;
        private Stream        _stream;
        private string        _username;
        private SecureString  _password;

        private static int SocketTimeout = 10;

        public NetClient(FooSyncEngine foo, string hostname, int port, string username, SecureString password, string repoName = null)
        {
            _foo      = foo;
            _hostname = hostname;
            _port     = port;
            _username = username;
            _password = password;
            _repoName = repoName;
            _client   = null;
            _stream   = null;
        }

        private TcpClient GetClient()
        {
            var client = new TcpClient();
            IAsyncResult ar = client.BeginConnect(_hostname, _port, null, null);

            try
            {
                if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(SocketTimeout), false))
                {
                    client.Close();
                    throw new TimeoutException(string.Format("Connecting to host {0}:{1} took too long.", _hostname, _port));
                }
                client.EndConnect(ar);
            }
            catch (Exception)
            {
                client.Close();
                throw;
            }

            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;

            return client;
        }

        public ICollection<string> ListRepositories()
        {
            var list = new List<string>();

            var client = GetClient();
            var stream = client.GetStream();

            NetUtil.WriteOpCode(stream, OpCode.ListRepos);
            NetUtil.WriteString(stream, _username);
            NetUtil.WriteString(stream, _password.ToString());

            int i = NetUtil.GetInt(stream);
            if (i != (int)RetCode.Success)
                throw new AuthException(string.Format("Authentication with FooSync server failed: code {0}", ((RetCode)i).ToString()));

            i = NetUtil.GetInt(stream);
            while (i-- >= 0)
            {
                var s = NetUtil.GetString(stream);
                list.Add(s);
            }

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

            _client = GetClient();
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

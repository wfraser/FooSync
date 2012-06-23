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
        private BinaryReader  _reader;
        private BinaryWriter  _writer;
        private string        _username;
        private SecureString  _password;

        private static int SocketTimeout = 10;

        public string Hostname { get { return _hostname; } }
        public int Port { get { return _port; } }
        public string Username { get { return _username; } }
        public string RepoName { get { return _repoName; } set { _repoName = value; } }

        public string ReportedHostname { get; private set; }
        public string ServerDescription { get; private set; }
        public Version ServerProtocolVersion { get; private set; }
        public Version ServerSoftwareVersion { get; private set; }

        public static readonly Version MinProtocolVersion = new Version(0,0);
        public static readonly Version MaxProtocolVersion = new Version(0,0);

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
            _reader   = null;
            _writer   = null;

            ReportedHostname = null;
            ServerDescription = null;
            ServerProtocolVersion = null;
        }

        private TcpClient GetClient()
        {
            var client = new TcpClient();
            IAsyncResult ar = client.BeginConnect(_hostname, _port, null, null);

            if (!ar.CompletedSynchronously)
            {
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
            }

            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;

            return client;
        }

        private void EnsureConnected()
        {
            if (_client != null && _client.Connected)
                return;

            _client = GetClient();
            _stream = _client.GetStream();

            _stream.ReadTimeout = SocketTimeout * 1000;
            _stream.WriteTimeout = SocketTimeout * 1000;

            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);

            Auth();
        }

        public ICollection<string> ListRepositories()
        {
            var list = new List<string>();

            EnsureConnected();

            _writer.Write(OpCode.ListRepos);

            RetCode ret = _reader.ReadRetCode();
            if (ret != RetCode.Success)
                throw new AuthException(string.Format("Authentication with FooSync server failed: {0}", ret));

            int i = _reader.ReadInt32();
            while (i-- > 0)
            {
                var s = _reader.ReadString();
                list.Add(s);
            }

            return list;
        }

        public static bool IsDisconnect(Exception ex)
        {
            if (ex is EndOfStreamException)
            {
                return true;
            }
            else if (ex is IOException)
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
            RetCode ret;

            if (ServerProtocolVersion == null)
            {
                _writer.Write(OpCode.Hello);

                ret = _reader.ReadRetCode();
                if (ret != RetCode.Success)
                {
                    throw new AuthException(string.Format("Server didn't say hello: {0}", ret));
                }

                ServerProtocolVersion = new Version(_reader.ReadInt32(), _reader.ReadInt32());

                if (ServerProtocolVersion > MaxProtocolVersion)
                {
                    throw new AuthException(
                        string.Format(
                            "Server's protocol version is unsupported: {0} is higher than max supported version {1}",
                            ServerProtocolVersion,
                            MaxProtocolVersion));
                }

                if (ServerProtocolVersion < MinProtocolVersion)
                {
                    throw new AuthException(
                        string.Format(
                            "Server's protocol version is unsupported: {0} is lower than min supported version {1}",
                            ServerProtocolVersion,
                            MinProtocolVersion));
                }

                ReportedHostname = _reader.ReadString();
                ServerDescription = _reader.ReadString();

                ServerSoftwareVersion = new Version(_reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32());

                string helloString = _reader.ReadString();
            }

            _writer.Write(OpCode.Auth);
            _writer.Write(_username ?? "anonymous");
            _writer.Write(_password ?? new SecureString());

            ret = _reader.ReadRetCode();

            if (ret != RetCode.Success)
                throw new AuthException(string.Format("Authentication with FooSync server failed: {0}", ret));
        }

        public FooTree GetTree(Progress callback = null)
        {
            while (true)
            {
                try
                {
                    _writer.Write(OpCode.Tree);
                    int i = _reader.ReadInt32();

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

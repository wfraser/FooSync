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

        private TcpClient              _client;
        private SslStream              _ssl;
        private Stream                 _stream;
        private FooSyncEngine          _foo;
        private ServerRepositoryConfig _config;
        private Dictionary<string, ICollection<string>> _exceptions;
        private bool                   _authenticated = false;
        private string                 _repoName = string.Empty;

        public Session(TcpClient client, FooSyncEngine foo, ServerRepositoryConfig config, Dictionary<string, ICollection<string>> exceptions)
        {
            UseSSL = false; // default to false until this functionality actually works
            _client = client;
            _foo = foo;
            _config = config;
            _exceptions = exceptions;
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

            _stream.ReadTimeout = 2000;

            try
            {
                while (_client.Connected)
                {
                    //
                    // Handle any number of requests in the connection, as long as an auth request
                    // comes first and succeeds.
                    //

                    var opCode = (OpCode)NetUtil.GetInt(_stream);

                    if (opCode == OpCode.HttpGet)
                    {
                        HandleHttpGet();
                        _stream.Close();
                        break;
                    }

                    if (opCode == OpCode.HttpPost)
                    {
                        HandleHttpPost();
                        _stream.Close();
                        break;
                    }

                    if (opCode == OpCode.ListRepos)
                    {
                        HandleListRepos();
                        continue;
                    }

                    if (!_authenticated && opCode != OpCode.Auth)
                    {
                        NetUtil.WriteInt(_stream, (int)RetCode.BadAuth);
                        _client.Close();
                        return;
                    }

                    switch ((OpCode)opCode)
                    {
                        case OpCode.Hello:
                            HandleHelloRequest();
                            break;

                        case OpCode.Auth:
                            _authenticated = HandleAuthRequest();
                            break;

                        case OpCode.Tree:
                            HandleTreeRequest();
                            break;

                        case OpCode.State:
                            HandleStateRequest();
                            break;

                        case OpCode.GetFile:
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
                if (ex is IOException)
                {
                    var se = ex.InnerException as SocketException;
                    if (se != null && se.ErrorCode == 10053) // remote endpoint disconnected
                    {
                        return;
                    }
                }

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

        private void HandleHelloRequest()
        {
            NetUtil.WriteString(
                _stream,
                "Codewise.FooSync.Daemon says hello "
                    + (_client.Client.RemoteEndPoint as IPEndPoint).Address.ToString()
            );
        }

        /// <summary>
        /// Handle an authentication request. This must be the first request of a connection.
        /// 
        /// Currently just returns success.
        /// </summary>
        /// <returns>true if the user is authenticated; false otherwise</returns>
        private bool HandleAuthRequest()
        {
            _repoName = NetUtil.GetString(_stream);

            if (!_config.Repositories.ContainsKey(_repoName))
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadRepo);
                return false;
            }

            if (_config.Repositories[_repoName].AllowAllClients)
            {
                NetUtil.WriteInt(_stream, (int)RetCode.Success);
                return true;
            }

            // TODO
            
            NetUtil.WriteInt(_stream, (int)RetCode.BadAuth);

            return false;
        }

        /// <summary>
        /// List the repositories the user has access to.
        /// Writes return code 0 (success), the number of repos, and their names.
        /// </summary>
        private void HandleListRepos()
        {
            var repos = new List<string>();

            foreach (var repo in _config.Repositories)
            {
                if (repo.Value.AllowAllClients)
                {
                    repos.Add(repo.Key);
                }
                // TODO else: check client key
            }

            repos.Sort();

            NetUtil.WriteInt(_stream, (int)RetCode.Success);
            NetUtil.WriteInt(_stream, repos.Count);
            foreach (var repoName in repos)
            {
                NetUtil.WriteString(_stream, repoName);
            }
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
            if (!Directory.Exists(_config.Repositories[_repoName].Path))
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadPath);
                return;
            }

            NetUtil.WriteInt(_stream, (int)RetCode.Success);

            FooTree.ToStream(_foo, _config.Repositories[_repoName].Path, _exceptions[_repoName], _stream);
        }

        /// <summary>
        /// Handle a Repository State request
        /// 
        /// Gets a path from the stream, then writes a return code, the size of the state data,
        /// and then invokes RepositoryState.Write (which writes the same way it does to a file).
        /// </summary>
        private void HandleStateRequest()
        {
            var stateFile = Path.Combine(
                _config.Repositories[_repoName].Path,
                FooSyncEngine.RepoStateFileName
            );

            if (!File.Exists(stateFile))
            {
                var state = new RepositoryState();
                state.AddSource(new FooTree(_foo, _config.Repositories[_repoName].Path), RepositoryState.RepoSourceName);
                state.Write(stateFile);
            }

            NetUtil.WriteInt(_stream, (int)RetCode.Success);

            using (var statestream = new FileStream(stateFile, FileMode.Open))
            {
                NetUtil.WriteLong(_stream, statestream.Length);
                statestream.CopyTo(_stream);
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
            var filename = NetUtil.GetString(_stream);

            if (Path.DirectorySeparatorChar != '/')
            {
                filename = filename.Replace('/', Path.DirectorySeparatorChar);
            }

            var fullPath = Path.Combine(_config.Repositories[_repoName].Path, filename);

            try
            {
                //using (var file = new FileStream(fullPath, FileMode.Open))
                using (var file = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    NetUtil.WriteInt(_stream, (int)RetCode.Success);
                    NetUtil.WriteLong(_stream, file.Length);
                    file.CopyTo(_stream);
                }
            }
            catch (FileNotFoundException)
            {
                NetUtil.WriteInt(_stream, (int)RetCode.BadPath);
            }
        }

        /// <summary>
        /// Handles a HTTP GET request.
        /// TODO: this is just demo code
        /// </summary>
        private void HandleHttpGet()
        {
            var requestBytes = new List<byte>();
            while (requestBytes.Count < 2
                || !(    requestBytes[requestBytes.Count - 2] == '\r' 
                      && requestBytes[requestBytes.Count - 1] == '\n'))
            {
                requestBytes.Add((byte)_stream.ReadByte());
            }
            var request = Encoding.UTF8.GetString(requestBytes.ToArray());
            request = request.Substring(0, request.Length - (" HTTP/1.1\r\n".Length));

            string page, response, headers;

            if (request == "/")
            {
                page = "<html><body><h1>Hello!</h1></body></html>";
                response = "HTTP/1.0 200 OK\r\n";
                headers = "Content-Type: text/html\r\nContent-Length {0}\r\n\r\n";
            }
            else
            {
                page = "Bad path.";
                response = "HTTP/1.0 404 Not Found\r\n";
                headers = "Content-Type: text/plain\r\nContent-Length: {0}\r\n\r\n";
            }

            var buf = Encoding.UTF8.GetBytes(response);
            _stream.Write(buf, 0, buf.Length);

            var pageBytes = Encoding.UTF8.GetBytes(page);

            buf = Encoding.UTF8.GetBytes(
                string.Format(headers, pageBytes.Length));
            _stream.Write(buf, 0, buf.Length);

            _stream.Write(pageBytes, 0, pageBytes.Length);
        }

        private void HandleHttpPost()
        {
            //
            // until this is actually implemented...
            //

            _stream.ReadByte(); // consume the space after POST

            HandleHttpGet();
        }
    }
}

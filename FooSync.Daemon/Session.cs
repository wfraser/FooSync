﻿///
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security;
using System.Threading;
using System.Text;

namespace Codewise.FooSync.Daemon
{
    public class Session
    {
        public static readonly string AnonymousUsername = "anonymous";

        public bool UseSSL { get; set; }

        private TcpClient              _client;
        private SslStream              _ssl;
        private Stream                 _stream;
        private BinaryReader           _reader;
        private BinaryWriter           _writer;
        private FooSyncEngine          _foo;
        private ServerRepositoryConfig _config;
        private Dictionary<string, ICollection<string>> _exceptions;
        private bool                   _authenticated = false;
        private string                 _userName = AnonymousUsername;

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

            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);

#if !DEBUG
            _stream.ReadTimeout = 2000;
#endif

            try
            {
                while (_client.Connected)
                {
                    //
                    // Handle any number of requests in the connection, as long as an auth request
                    // comes first and succeeds.
                    //

                    var opCode = (OpCode)_reader.ReadInt32();

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

                    if (!_authenticated && opCode != OpCode.Auth)
                    {
                        _writer.Write(RetCode.BadAuth);
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

                        case OpCode.ListRepos:
                            HandleListRepos();
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
                            _writer.Write(RetCode.BadOp);
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
            _writer.Write(
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
            var username = _reader.ReadString();
            var password = _reader.ReadSecureString();

            var userSpec = _config.Users.SingleOrDefault(u => u.Name == username);
            if (userSpec == null || !CheckPassword(userSpec.Password, password))
            {
                _writer.Write(RetCode.BadAuth);
                return false;
            }
            else
            {
                _writer.Write(RetCode.Success);
                _userName = username;
                return true;
            }
        }

        private bool CheckPassword(UserSpec.UserSpecPassword expected, SecureString actual)
        {
            if (expected == null)
                return true;

            return true;
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
                // TODO check client auth
                repos.Add(repo.Key);
            }

            repos.Sort();

            _writer.Write(RetCode.Success);
            _writer.Write(repos.Count);
            foreach (var repoName in repos)
            {
                _writer.Write(repoName);
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
            var repoName = _reader.ReadString();

            if (!Directory.Exists(_config.Repositories[repoName].Path))
            {
                _writer.Write(RetCode.BadPath);
                return;
            }

            _writer.Write(RetCode.Success);

            FooTree.ToStream(_foo, _config.Repositories[repoName].Path, _exceptions[repoName], _stream);
        }

        /// <summary>
        /// Handle a Repository State request
        /// 
        /// Gets a path from the stream, then writes a return code, the size of the state data,
        /// and then invokes RepositoryState.Write (which writes the same way it does to a file).
        /// </summary>
        private void HandleStateRequest()
        {
            var repoName = _reader.ReadString();

            var stateFile = Path.Combine(
                _config.Repositories[repoName].Path,
                FooSyncEngine.RepoStateFileName
            );

            if (!File.Exists(stateFile))
            {
                var state = new RepositoryState();
                state.AddSource(new FooTree(_foo, _config.Repositories[repoName].Path), RepositoryState.RepoSourceName);
                state.Write(stateFile);
            }

            _writer.Write(RetCode.Success);

            using (var statestream = new FileStream(stateFile, FileMode.Open))
            {
                _writer.Write(statestream.Length);
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
            var repoName = _reader.ReadString();
            var filename = _reader.ReadString();

            if (Path.DirectorySeparatorChar != '/')
            {
                filename = filename.Replace('/', Path.DirectorySeparatorChar);
            }

            var fullPath = Path.Combine(_config.Repositories[repoName].Path, filename);

            try
            {
                //using (var file = new FileStream(fullPath, FileMode.Open))
                using (var file = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _writer.Write(RetCode.Success);
                    _writer.Write(file.Length);
                    file.CopyTo(_stream);
                }
            }
            catch (FileNotFoundException)
            {
                _writer.Write(RetCode.BadPath);
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

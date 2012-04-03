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

        public Session(TcpClient client)
        {
            UseSSL = false; // default to false until this functionality actually works
            _client = client;
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
        }
    }
}

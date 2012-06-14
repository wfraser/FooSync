///
/// Codewise/FooSync/FooSyncUrl.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Codewise.FooSync
{
    /// <summary>
    /// An extension of the System.Uri class that handles fs:// and file:/// URLs.
    /// Includes logic to take into account the FooSync daemon default port.
    /// 
    /// Note: do not include a username:password when using this class.
    /// </summary>
    public class FooSyncUrl : Uri
    {
        public static readonly string UriSchemeFooSync = "fs";
        public static readonly short  DefaultPort = 22022;

        private bool _isLocal;

        /// <summary>
        /// C'tor.
        /// </summary>
        /// <param name="url">file:/// or fs:// URL</param>
        public FooSyncUrl(string url)
            : base(url)
        {
            _isLocal = (Scheme == Uri.UriSchemeFile);

            if (!_isLocal && Scheme != UriSchemeFooSync)
            {
                throw new FormatException("Invalid URI schema; must be " + UriSchemeFooSync + " or " + Uri.UriSchemeFile + ", not " + Scheme);
            }

            if (UserInfo != string.Empty)
            {
                throw new FormatException("Don't use a username/password with a fs:// URL");
            }

            if (_isLocal)
            {
                if (!string.IsNullOrEmpty(Host))
                {
                    throw new FormatException("Can't have a hostname with a file:// URL");
                }
            }
            else if (string.IsNullOrEmpty(Host))
            {
                throw new FormatException("Hostname is required for fs:// URL");
            }
        }

        /// <summary>
        /// If file:/// URL, -1.
        /// If fs:// URL, the server port.
        /// </summary>
        public new int Port
        {
            get
            {
                return _isLocal ? -1 : ((base.Port == -1) ? DefaultPort : base.Port);
            }
        }

        /// <summary>
        /// If file:/// URL, true.
        /// If fs:// URL, whether the port is equal to FooSyncUrl.DefaultPort
        /// </summary>
        public new bool IsDefaultPort
        {
            get
            {
                return _isLocal || (Port == DefaultPort);
            }
        }

        /// <summary>
        /// Converts the FooSyncUrl instance into a URL string.
        /// Does not include the port if it is the default port.
        /// </summary>
        /// <returns>file:/// or fs:// URL string</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(Scheme)
              .Append("://")
              .Append(Host);

            if (!IsDefaultPort)
            {
                sb.Append(":")
                  .Append(Port);
            }

            // ensure triple-slash for path like "C:/..."
            if (!AbsolutePath.StartsWith("/"))
                sb.Append("/");

            sb.Append(AbsolutePath);

            return sb.ToString();
        }

        /// <summary>
        /// Compare two FooSyncUrl instances for equality.
        /// Equality is considered host, port, and path being equal.
        /// </summary>
        /// <param name="comparand"></param>
        /// <returns></returns>
        public override bool Equals(object comparand)
        {
            var other = comparand as FooSyncUrl;
            if (other == null)
            {
                return false;
            }

            if (this.Host.Equals(other.Host)
                && this.Port.Equals(other.Port)
                && this.AbsolutePath.Equals(other.AbsolutePath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

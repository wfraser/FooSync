using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Codewise.FooSync
{
    public class FooSyncUrl : Uri
    {
        public static readonly string UriSchemeFooSync = "fs";
        public static readonly int DefaultPort = 22022;

        private bool _isLocal;

        public FooSyncUrl(string url)
            : base(url)
        {
            _isLocal = (Scheme == Uri.UriSchemeFile);

            if (!_isLocal && Scheme != UriSchemeFooSync)
            {
                throw new FormatException("Invalid URI schema; must be " + UriSchemeFooSync + " or " + Uri.UriSchemeFile + ", not " + Scheme);
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

        public new int Port
        {
            get
            {
                return _isLocal ? -1 : ((base.Port == -1) ? DefaultPort : base.Port);
            }
        }

        public new bool IsDefaultPort
        {
            get
            {
                return _isLocal || (Port == DefaultPort);
            }
        }

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
    }
}

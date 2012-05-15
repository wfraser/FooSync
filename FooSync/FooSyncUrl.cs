using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codewise.FooSync
{
    public class FooSyncUrl : Uri
    {
        public static readonly string UriSchemeFooSync = "fs";
        public static readonly int DefaultPort = 22022;

        public FooSyncUrl(string url)
            : base(url)
        {
            if (Scheme != UriSchemeFooSync)
            {
                throw new FormatException("Invalid URI schema; must be " + UriSchemeFooSync + ", not " + Scheme);
            }

            if (string.IsNullOrEmpty(Host))
            {
                throw new FormatException("Empty hostname");
            }
        }

        public new int Port
        {
            get
            {
                return (base.Port == -1) ? DefaultPort : base.Port;
            }
        }

        public new bool IsDefaultPort
        {
            get
            {
                return (Port == DefaultPort);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("fs://")
              .Append(Host);

            if (!IsDefaultPort)
            {
                sb.Append(":")
                  .Append(Port);
            }

            sb.Append(AbsolutePath);

            return sb.ToString();
        }
    }
}

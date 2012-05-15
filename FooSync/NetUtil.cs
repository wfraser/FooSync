///
/// Codewise/FooSync/NetUtil.cs
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
using System.Text;

namespace Codewise.FooSync
{
    /// <summary>
    /// Utility functions for writing data to a network stream.
    /// </summary>
    public static class NetUtil
    {
        public static int GetInt(Stream s)
        {
            var buf = new byte[4];
            s.Read(buf, 0, 4);

            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));
        }

        public static long GetLong(Stream s)
        {
            var buf = new byte[8];
            s.Read(buf, 0, 8);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buf, 0));
        }

        public static string GetString(Stream s)
        {
            var buf = new byte[4];
            s.Read(buf, 0, 4);
            int strLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buf, 0));

            buf = new byte[strLen];
            s.Read(buf, 0, strLen);
            return Encoding.UTF8.GetString(buf);
        }

        public static void WriteInt(Stream s, int i)
        {
            s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i)), 0, 4);
        }

        public static void WriteLong(Stream s, long l)
        {
            s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(l)), 0, 8);
        }

        public static void WriteString(Stream s, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);

            WriteInt(s, bytes.Length);
            s.Write(bytes, 0, bytes.Length);
        }

        public static void WriteOpCode(Stream s, OpCode o)
        {
            WriteInt(s, (int)o);
        }
    }
}

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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Codewise.FooSync
{
    /// <summary>
    /// Utility functions for writing data to a network stream.
    /// </summary>
    public static class NetUtil
    {
        public static SecureString ReadSecureString(this BinaryReader reader)
        {
            var secure = new SecureString();
            int nChars = reader.ReadInt32(); // note, this is NOT the goofy 7-bit thing

            for (int i = 0; i < nChars; i++)
            {
                byte low  = reader.ReadByte();
                byte high = reader.ReadByte();
                char c = (char)((int)low + ((int)high << 8));
                secure.AppendChar(c);
            }

            return secure;
        }

        public static void Write(this BinaryWriter writer, SecureString secure)
        {
            writer.Write(secure.Length); // note, this is NOT the goofy 7-bit thing

            var ptr = Marshal.SecureStringToBSTR(secure);
            int len = Marshal.ReadInt32(ptr, -4);
            for (int i = 0; i < len; i++)
            {
                byte b = Marshal.ReadByte(ptr, i);
                writer.Write(b);
            }

            Marshal.ZeroFreeBSTR(ptr);
        }

        public static void Write(this BinaryWriter writer, OpCode o)
        {
            writer.Write((int)o);
        }

        public static void Write(this BinaryWriter writer, RetCode r)
        {
            writer.Write((int)r);
        }
    }
}

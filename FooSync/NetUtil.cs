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
        public static OpCode ReadOpCode(this BinaryReader reader)
        {
            return (OpCode)reader.ReadInt32();
        }

        public static RetCode ReadRetCode(this BinaryReader reader)
        {
            return (RetCode)reader.ReadInt32();
        }

        public static void Write(this BinaryWriter writer, OpCode o)
        {
            writer.Write((int)o);
        }

        public static void Write(this BinaryWriter writer, RetCode r)
        {
#if DEBUG
            Console.WriteLine("return: {0}", r);
#endif
            writer.Write((int)r);
        }

        /// <summary>
        /// Note: this doesn't keep the SecureString secure.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="secure"></param>
        public static void Write(this BinaryWriter writer, SecureString secure)
        {
            var bwx = new BinaryWriterEx(writer.BaseStream);
            
            byte[] utf16 = new byte[secure.Length * 2];

            var ptr = Marshal.SecureStringToBSTR(secure);
            var len = Marshal.ReadInt32(ptr, -4);

            for (int i = 0; i < len; i += 2)
            {
                utf16[i] = Marshal.ReadByte(ptr, i);
            }

            Marshal.ZeroFreeBSTR(ptr);

            byte[] utf8 = UTF8Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16);

            for (int i = 0; i < utf16.Length; i++)
            {
                utf16[i] = 0; // clear memory
            }

            bwx.Write7BitEncodedInt(utf8.Length);
            for (int i = 0; i < utf8.Length; i++)
            {
                bwx.Write(utf8[i]);
                utf8[i] = 0;
            }
        }

        private class BinaryWriterEx : BinaryWriter
        {
            public BinaryWriterEx(Stream stream)
                : base(stream)
            {
            }

            //
            // Make this a public method.
            //
            public new void Write7BitEncodedInt(Int32 i)
            {
                base.Write7BitEncodedInt(i);
            }
        }
    }
}

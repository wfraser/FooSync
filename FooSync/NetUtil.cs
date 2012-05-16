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
            writer.Write((int)r);
        }
    }
}

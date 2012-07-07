///
/// Codewise/FooSync/Options.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;

namespace Codewise.FooSync
{
    public class Options
    {
        public Options()
        {
            CaseInsensitive = true;
        }

        public bool CaseInsensitive { get; set; }
    }
}

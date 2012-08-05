///
/// Codewise/FooSync/WPFApp/RepositoryDiffData.cs
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

namespace Codewise.FooSync.WPFApp
{
    public class RepositoryDiffData : List<RepositoryDiffDataItem>
    {
    }

    public class RepositoryDiffDataItem
    {
        public string Filename { get; set; }
        public string State { get; set; }
    }
}

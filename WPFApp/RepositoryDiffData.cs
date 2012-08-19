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
        public RepositoryDiffDataItem()
        {
            ChangeStatus = new Dictionary<Guid, ChangeStatus>();
            FileOperation = new Dictionary<Guid, FileOperation>();
        }

        public string Filename { get; set; }
        public string State { get; set; }
        public Dictionary<Guid, ChangeStatus> ChangeStatus { get; private set; }
        public Dictionary<Guid, FileOperation> FileOperation { get; private set; }
    }
}

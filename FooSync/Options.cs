using System;

namespace FooSync
{
    public class Options
    {
        public Options()
        {
            ComputeHashes = true;
            SearchHashes = true;
            CaseInsensitive = true;
        }

        public bool ComputeHashes { get; set; }
        public bool SearchHashes { get; set; }
        public bool CaseInsensitive { get; set; }
    }
}

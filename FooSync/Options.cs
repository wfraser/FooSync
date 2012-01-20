﻿using System;

namespace FooSync
{
    public class Options
    {
        public Options()
        {
            ComputeHashes = true;
            SearchHashes = true;
        }

        public bool ComputeHashes { get; set; }
        public bool SearchHashes { get; set; }
    }
}

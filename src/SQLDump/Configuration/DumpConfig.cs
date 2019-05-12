﻿using System.Collections.Generic;

namespace SQLDump.Configuration
{
    public class DumpConfig
    {
        public string ConnectionString { get; set; }
        public string OutputDirectory { get; set; }
        public int? Limit { get; set; }
        public bool IncludeIdentityInsert { get; set; }
        public bool TableListIsExclusive { get; set; }
        public List<string> TableNames { get; set; }
        public TableInfo[] TableInfos { get; set; }
        public string FileNamePrefix { get; set; }
        public string FileNameSuffix { get; set; }
    }
}
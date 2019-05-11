using System.Collections.Generic;

namespace SQLDump.Configuration
{
    public class Options
    {
        public string ConnectionString { get; set; }
        public int? Limit { get; set; }
        public string OutputDirectory { get; set; }
        public bool IncludeIdentityInsert { get; set; }
        public bool ListIsExclusive { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public List<string> TableNames { get; set; }
        public string ConfigPath { get; set; }
        public string FileNamePrefix { get; set; }
        public string FileNameSuffix { get; set; }
    }
}
using System.Collections.Generic;

namespace SQLDump.Configuration
{
    public class DumpRequest
    {
        public string ConnectionString { get; set; }
        public string OutputDirectory { get; set; }
        public bool ListIsExclusive { get; set; }
        public List<TableInfo> TableRequests { get; set; }
        public string FileNamePrefix { get; set; }
        public string FileNameSuffix { get; set; }
    }
}

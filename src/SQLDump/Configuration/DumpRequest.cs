using System.Collections.Generic;

namespace SQLDump.Configuration
{
    public class DumpRequest
    {
        public string ConnectionString { get; set; }

        public bool ListIsExclusive { get; set; }

        public string OutputDirectory { get; set; }

        public string FileNamePrefix { get; set; }

        public string FileNameSuffix { get; set; }

        public List<TableRequest> TableRequests { get; set; }
    }
}

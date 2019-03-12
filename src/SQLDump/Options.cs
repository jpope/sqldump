using System.Collections.Generic;

namespace SQLDump
{
    public class Options
    {
        public string Server { get; set; }
        public string Database { get; set; }
        public bool UseSqlServerAuthenication { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int? Limit { get; set; }
        //            public string OutputDirectory { get; set; }
        public bool UseTransaction { get; set; }
        public bool IncludeIdentityInsert { get; set; }
        public bool ListIsExclusive { get; set; }
        public bool ShowHelp { get; set; }
        public bool ShowVersion { get; set; }
        public List<string> TableNames { get; set; }
        public string ConfigPath { get; set; }
    }
}
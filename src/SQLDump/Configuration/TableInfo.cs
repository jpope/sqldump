namespace SQLDump.Configuration
{
    public class TableInfo
    {
        public string Name { get; set; }
        public string IdentityColumn { get; set; }
        public int? Limit { get; set; }
        public bool IncludeIdentityInsert { get; set; }
    }
}
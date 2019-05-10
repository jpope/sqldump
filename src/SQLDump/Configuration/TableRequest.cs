namespace SQLDump.Configuration
{
    public class TableRequest : TableInfo
    {
        public int? Limit { get; set; }

        public bool IncludeIdentityInsert { get; set; }
    }
}
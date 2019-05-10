namespace SQLDump.Configuration
{
    internal class TableRequest : TableInfo
    {
        public int? Limit { get; set; }

        public bool IncludeIdentityInsert { get; set; }
    }
}
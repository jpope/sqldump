namespace SQLDump.Configuration
{
    public class TableInfo
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public string IdentityColumn { get; set; }
        public string SchemaAndTableName
        {
            get { return $"[{Schema}].[{Name}]"; }
        }
        public int? Limit { get; set; }
        public bool IncludeIdentityInsert { get; set; }
    }
}
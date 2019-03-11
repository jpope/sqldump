namespace SQLDump.Configuration
{
    internal class TableRequest
    {
        public string Name { get; set; }

        public string IdentityColumn { get; set; }

        public int? Limit { get; set; }

        public bool IncludeIdentityInsert { get; set; }
    }
}
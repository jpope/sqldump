using System.Collections.Generic;

namespace SQLDump.Configuration
{
    public class TableInfo
    {
        public TableInfo()
        {
            OverrideColumns = new List<ColumnInfo>();
        }
        public string Name { get; set; }
        public string Schema { get; set; }
        public string IdentityColumn { get; set; }
        public string SchemaAndTableName
        {
            get { return $"[{Schema}].[{Name}]"; }
        }

        public List<ColumnInfo> OverrideColumns { get; set; }
    }

}
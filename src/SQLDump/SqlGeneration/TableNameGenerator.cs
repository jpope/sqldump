using System.Collections.Generic;
using System.Data;
using System.Linq;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableNameGenerator
    {
        public static IEnumerable<TableInfo> GetTablesToDump(IDbConnection connection, ICollection<string> tableNames, bool listIsExclusive)
        {
            const string sqlFormat =
                @"select
	t.table_name,
	t.table_schema,
    (select top 1
		c.column_name
	from
		information_schema.columns c
	where
		c.table_name = t.table_name
		and columnproperty(object_id(c.table_name), c.column_name, 'IsIdentity') = 1
	) as identity_column
from
	information_schema.tables t
where
	t.table_type = 'BASE TABLE'{0}
order by
	t.table_name";

            string sql;

            if (tableNames.Count == 0)
            {
                sql = string.Format(sqlFormat, string.Empty);
            }
            else if (listIsExclusive)
            {
                sql = string.Format(sqlFormat, "\n\tand t.table_name not in ('" + string.Join("', '", tableNames) + "')");
            }
            else
            {
                sql = string.Format(sqlFormat, "\n\tand t.table_name in ('" + string.Join("', '", tableNames) + "')");
            }

            var tableList = new List<TableInfo>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tableName = reader.GetString(0);
                        var tableSchema = reader.GetString(1);
                        var identityColumn = reader.IsDBNull(2) ? null : reader.GetString(2);

                        tableList.Add(new TableInfo
                        {
                            Name = tableName,
                            Schema = tableSchema,
                            IdentityColumn = identityColumn
                        });
                    }
                }
            }
            var sortedTableList = new List<TableInfo>();

            if (tableNames.Count > 1)
            {
                foreach (var tableName in tableNames)
                {
                    var tableInfo = tableList.Single(x => x.Name == tableName);
                    tableList.Remove(tableInfo);
                    sortedTableList.Add(tableInfo);
                }
            }
            else
            {
                sortedTableList = tableList;
            }

            return sortedTableList;
        }

    }
}
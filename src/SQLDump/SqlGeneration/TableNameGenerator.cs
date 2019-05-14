using System.Collections.Generic;
using System.Data;
using System.Linq;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableNameGenerator
    {
        public static IEnumerable<TableInfo> GetTablesToDump(IDbConnection connection, DumpConfig dumpConfig)
        {
            const string sqlFormat =
                @"select
	t.TABLE_NAME,
	t.TABLE_SCHEMA,
    (select top 1
		c.COLUMN_NAME
	from
		information_schema.columns c
	where
		c.TABLE_NAME = t.TABLE_NAME
		and columnproperty(object_id(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 1
	) as identity_column
from
	information_schema.tables t
where
	t.TABLE_TYPE = 'BASE TABLE'{0}
order by
	t.TABLE_NAME";

            var tableNames = dumpConfig.TableInfos.Select(x => x.Name).ToList();
            var listIsExclusive = dumpConfig.TableListIsExclusive;

            string sqlTableNames;

            if (tableNames.Count == 0)
            {
                sqlTableNames = string.Format(sqlFormat, string.Empty);
            }
            else if (listIsExclusive)
            {
                sqlTableNames = string.Format(sqlFormat,
                    "\n\tand t.table_name not in ('" + string.Join("', '", tableNames) + "')");
            }
            else
            {
                sqlTableNames = string.Format(sqlFormat,
                    "\n\tand t.table_name in ('" + string.Join("', '", tableNames) + "')");
            }

            var tableList = new List<TableInfo>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sqlTableNames;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tableName = reader.GetString(0);
                        var tableSchema = reader.GetString(1);
                        var identityColumn = reader.IsDBNull(2) ? null : reader.GetString(2);

                        var passedInTable = dumpConfig
                            .TableInfos
                            .SingleOrDefault(x => x.Name == tableName);

                        var tableInfo = new TableInfo
                        {
                            Name = tableName,
                            Schema = tableSchema,
                            IdentityColumn = identityColumn
                        };

                        if (passedInTable != null && passedInTable.OverrideColumns.Any())
                        {
                            tableInfo.OverrideColumns = passedInTable.OverrideColumns;
                        }

                        tableList.Add(tableInfo);
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

            foreach (var tableInfo in sortedTableList)
            {
                AddColumnFormatInfo(connection, tableInfo);
            }

            return sortedTableList;
        }

        private static void AddColumnFormatInfo(IDbConnection connection, TableInfo tableInfo)
        {
            var sqlColumnInfo =
                $@"SELECT COLUMN_NAME, 'Date' + CASE WHEN IS_NULLABLE = 'YES' THEN '?' ELSE '' END AS 'ColumnType'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE DATA_TYPE = 'date' AND TABLE_SCHEMA = '{tableInfo.Schema}' AND TABLE_NAME = '{tableInfo.Name}'";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sqlColumnInfo;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(0);
                        var columnType = reader.GetString(1);

                        if (tableInfo.OverrideColumns.Any(x => x.ColumnName == columnName))
                            continue;

                        var columnInfo = new ColumnInfo
                        {
                            ColumnName = columnName,
                            OverrideSerializationType = columnType
                        };

                        tableInfo.OverrideColumns.Add(columnInfo);
                    }
                }
            }
        }
    }
}
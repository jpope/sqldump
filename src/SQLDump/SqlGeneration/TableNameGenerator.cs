using System.Collections.Generic;
using System.Data;
using System.Linq;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableNameGenerator
    {
        public static IEnumerable<TableInfo> GetTablesToDump(IDbConnection connection, DumpRequest dumpRequest)
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

            string sql;
            var tableInfoInPart = string.Join(",", dumpRequest.TableRequests.Select(t => $"'{t.Name}'"));

            if (dumpRequest.TableRequests.Count == 0)
            {
                sql = string.Format(sqlFormat, string.Empty);
            }
            else if (dumpRequest.ListIsExclusive)
            {
                sql = string.Format(sqlFormat, $"and CONCAT(t.table_schema, '.', t.table_name) NOT IN ( {tableInfoInPart} )");
            }
            else
            {
                sql = string.Format(sqlFormat, $"and CONCAT(t.table_schema, '.', t.table_name) IN ( {tableInfoInPart} )");
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

                        foreach (var t in dumpRequest.TableRequests)
                        {
                            var fullName = $"{tableSchema}.{tableName}";
                            if (t.Name != fullName)
                            {
                                continue;
                            }

                            tableList.Add(new TableInfo
                            {
                                IdentityColumn = identityColumn,
                                Name = fullName,
                                IncludeIdentityInsert = t.IncludeIdentityInsert,
                                Limit = t.Limit
                            });

                            break;
                        }
                    }
                }
            }

            return tableList;
        }

    }
}
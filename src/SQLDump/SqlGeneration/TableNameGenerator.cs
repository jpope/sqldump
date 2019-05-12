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
            const string sqlFormat = @"select
	                t.table_name,
	                (select top 1
		                c.column_name
	                from
		                information_schema.columns c
	                where c.table_name = t.table_name
		                and columnproperty(object_id(c.table_schema+'.'+c.table_name), c.column_name, 'IsIdentity') = 1
	                ) as identity_column,
	                table_schema
                from
	                information_schema.tables t
                where t.table_type = 'BASE TABLE'
                      {0}
                order by
	                t.table_name";

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
                        var identityColumn = reader.IsDBNull(1) ? null : reader.GetString(1);
                        var schemaName = reader.GetString(2);
                        foreach (var t in dumpRequest.TableRequests)
                        {
                            var fullName = $"{schemaName}.{tableName}";
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
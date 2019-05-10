using Newtonsoft.Json;
using SQLDump.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using SQLDump.SqlGeneration;

namespace SQLDump
{
    internal static class Program
    {
        private const string DefaultRequestsFile = "requests.json";

        private static int Main(string[] args)
        {
            try
            {
                var fileName = DefaultRequestsFile;
                if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                {
                    fileName = args[0];
                }

                Console.WriteLine($"Reading requests file: '{fileName}'");
                var requestString = File.ReadAllText(fileName);
                var requests = JsonConvert.DeserializeObject<DumpRequest[]>(requestString);
                Console.WriteLine($"Requests to process: '{requests.Length}'");

                foreach (var request in requests)
                {
                    Console.WriteLine($"Processing request. ConnectionString: '{request.ConnectionString}'; Tables requets: {request.TableRequests.Count}");
                    PerformDump(request);
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.ToString());
                return 1;
            }

            return 0;
        }

        private static void PerformDump(DumpRequest dumpRequest)
        {
            using (var connection = new SqlConnection(dumpRequest.ConnectionString))
            {
                connection.Open();

                var tablesToDump = GetTablesToDump(connection, dumpRequest).ToList();
                Console.WriteLine($"Processing request. ConnectionString: '{dumpRequest.ConnectionString}'; Tables requests verified: {tablesToDump.Count}");
                for (var i = 0; i < tablesToDump.Count; i++)
                {
                    Console.WriteLine($"Processing table request. Name: {tablesToDump[i].Name}");
                    DumpTable(connection, tablesToDump[i], dumpRequest, i);
                }
            }
        }

        private static IEnumerable<TableRequest> GetTablesToDump(IDbConnection connection, DumpRequest dumpRequest)
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

            var tableList = new List<TableRequest>();
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

                            tableList.Add(new TableRequest
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

        private static void DumpTable(IDbConnection connection, TableRequest tableRequest, DumpRequest dumpRequest, int fileNumber)
        {
            if (!Directory.Exists(dumpRequest.OutputDirectory))
            {
                Directory.CreateDirectory(dumpRequest.OutputDirectory);
            }

            var fileNamePrefix = dumpRequest.FileNamePrefix + fileNumber.ToString("D2");
            var writer = new FileInfo($"{dumpRequest.OutputDirectory}/{fileNamePrefix}_{tableRequest.Name.Replace(".", "-")}_{dumpRequest.FileNameSuffix}.sql").CreateText();
            writer.AutoFlush = true;
            if (tableRequest.IncludeIdentityInsert && (tableRequest.IdentityColumn != null))
            {
                writer.WriteLine("set identity_insert " + tableRequest.Name + " on");
                writer.WriteLine();
            }

            using (var command = connection.CreateCommand())
            {
                var top = tableRequest.Limit.HasValue && tableRequest.Limit.Value > 0
                    ? $"top {tableRequest.Limit.Value}"
                    : null;

                command.CommandText = $"select {top} * from {tableRequest.Name}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        writer.WriteLine(GetInsertStatement(tableRequest, reader, tableRequest.IncludeIdentityInsert));
                    }
                }
            }

            if (tableRequest.IncludeIdentityInsert && (tableRequest.IdentityColumn != null))
            {
                writer.WriteLine();
                writer.WriteLine("set identity_insert " + tableRequest.Name + " off");
            }

            writer.Close();
        }

        private static string GetInsertStatement(TableRequest table, IDataRecord reader, bool includeIdentityInsert)
        {
            var builder = new StringBuilder("");
            builder.Append("insert into " + table.Name + " (");
            var flag = true;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var name = reader.GetName(i);
                if (includeIdentityInsert || (name != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }

                    builder.Append("[" + name + "]");
                }
            }

            builder.Append(") values (");
            flag = true;
            for (var j = 0; j < reader.FieldCount; j++)
            {
                var str2 = reader.GetName(j);
                if (includeIdentityInsert || (str2 != table.IdentityColumn))
                {
                    if (flag)
                    {
                        flag = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    string str3 = ConvertToSqlLiteral(reader.GetFieldType(j), reader.GetValue(j));
                    builder.Append(str3);
                }
            }

            builder.Append(")");
            return builder.ToString();
        }

        private static string ConvertToSqlLiteral(Type type, object value)
        {
            return SqlGenerator.ConvertToSqlLiteral(type, value);
        }

        private static void PrintError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.Error.WriteLine("ERROR: " + message);

            Console.ForegroundColor = originalColor;
        }
    }
}
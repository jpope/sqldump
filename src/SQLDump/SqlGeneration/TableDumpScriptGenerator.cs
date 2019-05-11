using System;
using System.Data;
using System.IO;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableDumpScriptGenerator
    {
        public static void DumpTable(IDbConnection connection, TableInfo table, bool includeIdentityInsert, int? limit, string filePath)
        {
            var writer = new FileInfo(filePath).CreateText();
            writer.AutoFlush = true;

            var schemaAndTable = table.SchemaAndTableName;

            if (includeIdentityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine($"set identity_insert {schemaAndTable} on");
                writer.WriteLine();
            }
            using (var command = connection.CreateCommand())
            {
                var limitClause = "";

                if (limit.HasValue)
                {
                    limitClause = $"LIMIT {limit} ";
                }

                command.CommandText = $"SELECT {limitClause}* FROM {schemaAndTable} ";
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        writer.WriteLine(SqlGenerator.GetInsertStatement(table, reader, includeIdentityInsert));
                    }
                }
            }
            if (includeIdentityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine();
                writer.WriteLine($"set identity_insert {schemaAndTable} off");
            }
            writer.Close();
        }
    }
}
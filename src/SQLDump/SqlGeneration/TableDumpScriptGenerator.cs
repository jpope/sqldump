using System;
using System.Data;
using System.IO;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableDumpScriptGenerator
    {
        public static void DumpTable(IDbConnection connection, DumpConfig dumpConfig, TableInfo table, string filePath)
        {
            var identityInsert = dumpConfig.IncludeIdentityInsert;
            var limit = dumpConfig.Limit;

            var writer = new FileInfo(filePath).CreateText();
            writer.AutoFlush = true;

            var schemaAndTable = table.SchemaAndTableName;

            if (identityInsert && (table.IdentityColumn != null))
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
                        writer.WriteLine(SqlGenerator.GetInsertStatement(table, reader, identityInsert));
                    }
                }
            }
            if (identityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine();
                writer.WriteLine($"set identity_insert {schemaAndTable} off");
            }
            writer.Close();
        }
    }
}
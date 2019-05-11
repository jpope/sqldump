using System;
using System.Data;
using System.IO;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableDumpScriptGenerator
    {
        public static void DumpTable(IDbConnection connection, TableInfo table, bool includeIdentityInsert, int? limit, string outputDirectory, int iFile, string nameSuffix, string namePrefix)
        {
            var filePath = outputDirectory + "/" + namePrefix + table.Name + nameSuffix + ".sql";

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
                if (limit.HasValue)
                {
                    command.CommandText = string.Concat(new object[] { "select top ", limit, " * from ", schemaAndTable });
                }
                else
                {
                    command.CommandText = "select * from " + schemaAndTable;
                }
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
using System;
using System.Data;
using System.IO;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableDumpScriptGenerator
    {
        public static void DumpTable(IDbConnection connection, TableInfo table, DumpRequest dumpRequest, int iFile)
        {
            EnsureDirectoryExists(dumpRequest.OutputDirectory);

            var fileNamePrefix = dumpRequest.FileNamePrefix + iFile.ToString("D2");
            var writer = new FileInfo($"{dumpRequest.OutputDirectory}/{fileNamePrefix}_{table.Name.Replace(".", "-")}_{dumpRequest.FileNameSuffix}.sql").CreateText();
            writer.AutoFlush = true;
            if (table.IncludeIdentityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine("set identity_insert " + table.Name + " on");
                writer.WriteLine();
            }

            using (var command = connection.CreateCommand())
            {
                var top = table.Limit.HasValue && table.Limit.Value > 0
                    ? $"top {table.Limit.Value}"
                    : null;

                command.CommandText = $"select {top} * from {table.Name}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        writer.WriteLine(SqlGenerator.GetInsertStatement(table, reader, table.IncludeIdentityInsert));
                    }
                }
            }

            if (table.IncludeIdentityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine();
                writer.WriteLine("set identity_insert " + table.Name + " off");
            }

            writer.Close();
        }
        private static void EnsureDirectoryExists(string path)
        {
            var info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                info.Create();
            }
        }
    }
}
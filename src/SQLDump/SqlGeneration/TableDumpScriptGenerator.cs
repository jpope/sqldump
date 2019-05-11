﻿using System;
using System.Data;
using System.IO;
using SQLDump.Configuration;

namespace SQLDump.SqlGeneration
{
    public static class TableDumpScriptGenerator
    {
        public static void DumpTable(IDbConnection connection, TableInfo table, bool includeIdentityInsert, int? limit, string outputDirectory, int iFile)
        {
            EnsureDirectoryExists(outputDirectory);
            var fileNamePrefix = DateTime.Now.ToString("yyyy-MM-dd-HHmm.") + iFile.ToString("D2");
            var fileNameSuffix = ".ENV.DEV";

            var writer = new FileInfo(outputDirectory + "/" + fileNamePrefix + table.Name + fileNameSuffix + ".sql").CreateText();
            writer.AutoFlush = true;
            if (includeIdentityInsert && (table.IdentityColumn != null))
            {
                writer.WriteLine("set identity_insert [" + table.Name + "] on");
                writer.WriteLine();
            }
            using (var command = connection.CreateCommand())
            {
                if (limit.HasValue)
                {
                    command.CommandText = string.Concat(new object[] { "select top ", limit, " * from [", table.Name, "]" });
                }
                else
                {
                    command.CommandText = "select * from [" + table.Name + "]";
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
                writer.WriteLine("set identity_insert [" + table.Name + "] off");
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
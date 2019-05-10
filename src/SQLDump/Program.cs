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

                var tablesToDump = TableNameGenerator.GetTablesToDump(connection, dumpRequest).ToList();
                Console.WriteLine($"Processing request. ConnectionString: '{dumpRequest.ConnectionString}'; Tables requests verified: {tablesToDump.Count}");
                for (var i = 0; i < tablesToDump.Count; i++)
                {
                    Console.WriteLine($"Processing table request. Name: {tablesToDump[i].Name}");
                    DumpTable(connection, tablesToDump[i], dumpRequest, i);
                }
            }
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

        private static string GetInsertStatement(TableInfo table, IDataRecord reader, bool includeIdentityInsert)
        {
            return SqlGenerator.GetInsertStatement(table, reader, includeIdentityInsert);
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
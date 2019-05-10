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
                    TableDumpScriptGenerator.DumpTable(connection, tablesToDump[i], dumpRequest, i);
                }
            }
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
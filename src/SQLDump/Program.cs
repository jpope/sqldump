using Newtonsoft.Json;
using SQLDump.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SQLDump.SqlGeneration;

namespace SQLDump
{
    internal static class Program
    {
        private const string DefaultRequestsFile = "sqldump.config.json";

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
                    Console.WriteLine($"Processing request. ConnectionString: '{request.ConnectionString}'; Tables requests: {request.TableRequests.Count}");
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

        private static void PrintVersion()
        {
            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();

            if (version.Length % 2 == 1)
                version = string.Format(" SQLDump  {0} ", version);
            else
                version = string.Format(" SQLDump {0} ", version);

            var padding = new string(Enumerable.Repeat('8', (26 - version.Length) / 2).ToArray());

            var versionWithPadding = padding + version + padding;

            Console.WriteLine();
            Console.WriteLine(
                @"                        (                      
                         )                     
                    (   (                      
                     )   b                     
                    (    88_                   
                      ___888b__                
                    _d888888888b   (           
           (    ___d888888888888_   )          
            )  d88888888888888888b (           
           (  d8888888888888888888__           
           ___8888888888888888888888b          
          d{0}b         
          888888888888888888888888888P         
          Y8888888888888888888888888P          ", versionWithPadding);
        }
    }
}
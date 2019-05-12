using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using NDesk.Options;
using Newtonsoft.Json;
using SQLDump.Configuration;
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
	            var rawContent = File.ReadAllText(fileName);
	            var dumpConfig = JsonConvert.DeserializeObject<DumpConfig>(rawContent);

	            PerformDump(dumpConfig);

            }
            catch (Exception ex)
	        {
	            PrintError(ex.ToString());
	            return 1;
	        }

	        return 0;
		}

		private static void PerformDump(DumpConfig dumpConfig)
		{
		    EnsureDirectoryExists(dumpConfig.OutputDirectory);

		    using (var connection = new SqlConnection(dumpConfig.ConnectionString))
			{
				connection.Open();

				var tablesToDump = TableNameGenerator.GetTablesToDump(connection, dumpConfig.TableNames, dumpConfig.TableListIsExclusive);

			    var iFile = 1;
				var first = true;
				foreach (var table in tablesToDump)
				{
					if (first)
						first = false;
					else
						Console.WriteLine();

				    var fileNamePrefix = dumpConfig.FileNamePrefix + iFile.ToString("D3") + "_";

				    var filePath = dumpConfig.OutputDirectory + "/" + fileNamePrefix + table.Schema + "_" + table.Name + dumpConfig.FileNameSuffix + ".sql";
                    Console.WriteLine($"Creating file: {filePath}");

				    TableDumpScriptGenerator.DumpTable(connection, dumpConfig, table, filePath);
				    iFile++;
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

			var padding = new string(Enumerable.Repeat('8', (26 - version.Length)/2).ToArray());

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
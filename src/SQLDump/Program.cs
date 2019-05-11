﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NDesk.Options;
using SQLDump.Configuration;
using SQLDump.SqlGeneration;

namespace SQLDump
{
	internal static class Program
	{
	    private static int Main(string[] args)
		{
			var options = new Options();

			var optionSet = new OptionSet
			{
			    {"c|config-path", "read from a JSON configuration file at the given path (default is .\\)",
			        x => { options.ConfigPath = x; }},
                //Support for these has been broken
//			    { "i|use-integrated-security", "use Integrated Security to connect to server (default)", x => {}},
//			    {"s|use-sql-server-authentication", "use SQL Server authentication to connect to server", x => options.UseSqlServerAuthenication = x != null},
//			    {"u|username=", "username for SQL Server authentication", x => options.Username = x},
//			    {"p|password=", "password for SQL Server authentication", x => options.Password = x},
//			    {"l|limit=", "limit number of records per table", x => options.Limit = int.Parse(x)},
//			    {"t|use-transaction", "wrap all insert statements in a transaction", x => options.UseTransaction = x != null},
//			    {"d|identity-insert", "include statement to enable identity insert and include identity column in output", x => options.IncludeIdentityInsert = x != null},
//			    {"e|exclude", "supplied tables are excluded, rather than included", x => options.ListIsExclusive = x != null},
			    {"?|help", "display this help and exit", x => options.ShowHelp = x != null},
			    {"version", "output version information then exit", x => options.ShowVersion = x != null},
			};

			IList<string> arguments;

			try
			{
				arguments = optionSet.Parse(args);
			}
			catch (Exception ex)
			{
				PrintError(ex.ToString());
				return 1;
			}
//
			if (options.ShowHelp)
			{
				PrintHelp(optionSet);
				return 0;
			}
			else if (options.ShowVersion)
			{
				PrintVersion();
				return 0;
			}
//			else if (arguments.Count < 2)
//			{
//				PrintError("Not enough arguments supplied");
//				return 1;
//			}

		    options.ConnectionString = "Server=.\\;Database=DB_NAME_HERE;Trusted_Connection=True";
		    options.OutputDirectory = ConfigurationManager.AppSettings["OutputDirectory"];
            options.IncludeIdentityInsert = true;

            var myTableNames = new List<string>
		    {
		        "Table1",
		        "Table2",
            };

		    options.TableNames = myTableNames;

            try
			{
				PerformDump(options);
			}
			catch (Exception ex)
			{
				PrintError(ex.ToString());
				return 1;
			}

			return 0;
		}

		private static void PerformDump(Options options)
		{
			using (var connection = new SqlConnection(options.ConnectionString))
			{
				connection.Open();

				var tablesToDump = TableNameGenerator.GetTablesToDump(connection, options.TableNames ?? new List<string>(), options.ListIsExclusive);

			    var iFile = 1;
				var first = true;
				foreach (var table in tablesToDump)
				{
					if (first)
						first = false;
					else
						Console.WriteLine();

				    TableDumpScriptGenerator.DumpTable(connection, table, options.IncludeIdentityInsert, options.Limit, options.Database, options.OutputDirectory, iFile);
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

		private static void PrintHelp(OptionSet optionSet)
		{
			PrintVersion();

			var assemblyName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);

			Console.WriteLine();
			Console.WriteLine("Usage: " + assemblyName + " [OPTIONS] SERVER DATABASE [TABLES]");
			Console.WriteLine();
			Console.WriteLine("Options:");

			optionSet.WriteOptionDescriptions(Console.Out);
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
	}
}
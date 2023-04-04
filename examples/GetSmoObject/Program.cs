// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Linq;

namespace GetSmoObject
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 3) 
            {
                Console.WriteLine("GetSmoObject servername databasename \"URN of item to script\"");
                Console.WriteLine("\tExample: GetSmoObject myserver.database.windows.net mydatabase \"Server\\Database[@Name='mydatabase']\\Table[@Name='table' and @Schema='schema']\"");
                Console.WriteLine("\tSQL Server Management Studio users can find the URN of an object by expanding its child nodes in Object Explorer and examining the Output window content.")
                return;
            }
            var logger = new SqlClientEventLogger();
            try
            {
                using (var connection = new SqlConnection(new SqlConnectionStringBuilder()
                {
                    DataSource = args[0],
                    InitialCatalog = args[1],
                    Authentication = SqlAuthenticationMethod.ActiveDirectoryInteractive
                }.ConnectionString))
                {
                    var server = new Server(new ServerConnection(connection));
                    var obj = (SqlSmoObject)server.GetSmoObject(args[2]);
                    Console.WriteLine($"Found {obj.Urn}");
                    var scripter = new Scripter(server);
                    var script = scripter.Script(new[] { obj.Urn });
                    Console.WriteLine(string.Join(Environment.NewLine, script.Cast<string>().ToArray()));
                }
            }
            finally
            {
                Console.WriteLine(logger.ToString());
            }
        }
    }
}

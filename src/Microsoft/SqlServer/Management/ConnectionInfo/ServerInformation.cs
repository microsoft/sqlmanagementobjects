// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif


namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// Encapsulates server version, database engine type and database engine edition
    /// values into a single entity
    /// </summary>
    internal class ServerInformation
    {
        private readonly ServerVersion serverVersion;
        private readonly Version productVersion;
        private readonly DatabaseEngineType databaseEngineType;
        private readonly DatabaseEngineEdition databaseEngineEdition;
        private readonly string hostPlatform;
        private readonly NetworkProtocol connectionProtocol;

        /// <summary>
        /// Constructs a new ServerInformation object with the given values and HostPlatform of Windows
        /// Use this constructor only when the real value of the host platform isn't needed
        /// </summary>
        /// <param name="sv"></param>
        /// <param name="productVersion"></param>
        /// <param name="dt"></param>
        /// <param name="databaseEngineEdition"></param>
        public ServerInformation(ServerVersion sv, Version productVersion, DatabaseEngineType dt, DatabaseEngineEdition databaseEngineEdition)
            : this(sv, productVersion, dt, databaseEngineEdition, HostPlatformNames.Windows, NetworkProtocol.NotSpecified, isFabricServer: false)
        {

        }

        /// <summary>
        /// Constructs a new ServerInformation object with the given values
        /// </summary>
        /// <param name="sv"></param>
        /// <param name="productVersion"></param>
        /// <param name="dt"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <param name="hostPlatform"></param>
        /// <param name="connectionProtocol">net_transport value from dm_exec_connections for the current spid</param>
        /// <param name="isFabricServer"></param>
        public ServerInformation(ServerVersion sv, Version productVersion, DatabaseEngineType dt, DatabaseEngineEdition databaseEngineEdition,
            string hostPlatform, NetworkProtocol connectionProtocol, bool isFabricServer)
        {
            serverVersion = sv;
            this.productVersion = productVersion;
            databaseEngineType = dt;
            this.databaseEngineEdition = databaseEngineEdition;
            this.hostPlatform = hostPlatform;
            this.connectionProtocol = connectionProtocol;
            IsFabricServer = isFabricServer;
        }

        /// <summary>
        /// The host platform of the connection as given by select host_platform from sys.dm_os_host_info
        /// </summary>
        /// <remarks>Returns Windows prior to 2016 (when this DMV was introduced)</remarks>
        public string HostPlatform
        {
            get { return this.hostPlatform; }
        }

        /// <summary>
        /// The server version string given when this was initialized
        /// </summary>
        public ServerVersion ServerVersion
        {
            get { return serverVersion; }
        }

        /// <summary>
        /// The Product Version as given by SERVERPROPERTY('ProductVersion')
        /// </summary>
        public Version ProductVersion
        {
            get { return productVersion;  }
        }

        /// <summary>
        /// The DatabaseEngineType of the connection as given by SERVERPROPERTY('EDITION')
        /// </summary>
        public DatabaseEngineType DatabaseEngineType
        {
            get { return databaseEngineType; }
        }

        /// <summary>
        /// The DatabaseEngineEdition of the connection as given by SERVERPROPERTY('EngineEdition')
        /// </summary>
        public DatabaseEngineEdition DatabaseEngineEdition
        {
            get { return databaseEngineEdition; }
        }

        /// <summary>
        /// Protocol used for the connection. 
        /// </summary>
        public NetworkProtocol ConnectionProtocol
        {
            get { return connectionProtocol; }
        }

        /// <summary>
        /// Returns true if the server is running in Microsoft Fabric
        /// </summary>
        public bool IsFabricServer
        {
            get;
        }

        private static readonly HashSet<DatabaseEngineEdition> validEditions = new HashSet<DatabaseEngineEdition>(Enum.GetValues(typeof(DatabaseEngineEdition)).Cast<DatabaseEngineEdition>());
        // this query needs to be safe on all platforms. DW and Sql2005 don't support CONNECTIONPROPERTY
        private const string serverVersionQuery = @"DECLARE @edition sysname;
SET @edition = cast(SERVERPROPERTY(N'EDITION') as sysname);
SELECT case when @edition = N'SQL Azure' then 2 else 1 end as 'DatabaseEngineType',
SERVERPROPERTY('EngineEdition') AS DatabaseEngineEdition,
SERVERPROPERTY('ProductVersion') AS ProductVersion,
@@MICROSOFTVERSION AS MicrosoftVersion,
case when serverproperty('EngineEdition') = 12 then 1 when serverproperty('EngineEdition') = 11 and @@version like 'Microsoft Azure SQL Data Warehouse%' then 1 else 0 end as IsFabricServer;
";
        static public ServerInformation GetServerInformation(IDbConnection sqlConnection, IDbDataAdapter dataAdapter, string serverVersionString)
        {
            var serverVersion = ParseStringServerVersion(serverVersionString);
            var cmdBuilder = new StringBuilder(serverVersionQuery);

            if (serverVersion.Major >= 14)
            {
                cmdBuilder.AppendLine(@"select host_platform from sys.dm_os_host_info");
            }
            else
            {
                // Pre v14 is all Windows
                cmdBuilder.AppendLine(@"select N'Windows' as host_platform");
            }
                
            if (serverVersion.Major >= 10)
            {
                cmdBuilder.AppendLine(@"if @edition = N'SQL Azure' 
  select 'TCP' as ConnectionProtocol
else
  exec ('select CONVERT(nvarchar(40),CONNECTIONPROPERTY(''net_transport'')) as ConnectionProtocol')");
            }
            else
            {
                cmdBuilder.AppendLine("select NULL as ConnectionProtocol");
            }

            using (var sqlCommand = sqlConnection.CreateCommand())
            {
                sqlCommand.CommandText = cmdBuilder.ToString();
                var dataSet = new DataSet();
                dataAdapter.SelectCommand = sqlCommand;
                dataAdapter.Fill(dataSet);


                // NOTE: Dynamics CRM databases have unique restrictions (master not accessible, CREATE TABLE not permitted) that make
                // neither SqlDatabase or Unknown editions usable.  Because adding it as a defined DatabaseEngineEdition would result in
                // cascading changes elsewhere (query construction for SMO types, designer exemptions) and the previous behavior
                // (where this went unconverted) was functioning correctly for MSSQL and Azure Data Studio, we treat it as a special case here.
                const DatabaseEngineEdition dynamicsCrmEdition = (DatabaseEngineEdition)1000;

                var engineType = (DatabaseEngineType)dataSet.Tables[0].Rows[0]["DatabaseEngineType"];
                var edition = (DatabaseEngineEdition)dataSet.Tables[0].Rows[0]["DatabaseEngineEdition"];
                // Treat unknown editions from Azure the same as Azure SQL database
                if (engineType == DatabaseEngineType.SqlAzureDatabase && !validEditions.Contains(edition))
                {
                    if (edition != dynamicsCrmEdition) // Dynamics CRM databases have additional restrictions; don't treat them as Azure SQL DB.  See note above.
                    {
                        edition = DatabaseEngineEdition.SqlDatabase;
                    }
                }
                // If we're on Managed Instance, don't treat it as a "Sql Azure", but as "Standalone"
                // Also, determine the underlying engine version.
                //
                if (edition == DatabaseEngineEdition.SqlManagedInstance)
                {
                    engineType = DatabaseEngineType.Standalone;
                    serverVersion = ParseMicrosoftVersion(Convert.ToUInt32(dataSet.Tables[0].Rows[0]["MicrosoftVersion"].ToString()));
                }
                if (edition == DatabaseEngineEdition.SqlOnDemand)
                {
                    serverVersion = ParseMicrosoftVersion(Convert.ToUInt32(dataSet.Tables[0].Rows[0]["MicrosoftVersion"].ToString()));
                }
                var isFabricServer = Convert.ToBoolean(dataSet.Tables[0].Rows[0]["IsFabricServer"]);
                var connectionProtocol = dataSet.Tables[2].Rows[0]["ConnectionProtocol"];

                return new ServerInformation(serverVersion,
                    new Version((string)dataSet.Tables[0].Rows[0]["ProductVersion"]),
                    engineType,
                    edition,
                    (string)dataSet.Tables[1].Rows[0]["host_platform"],
                    connectionProtocol == DBNull.Value ? NetworkProtocol.NotSpecified : ProtocolFromNetTransport((string)connectionProtocol),
                    isFabricServer
                    );
            }
        }

        // Converts the net_transport column value from CONNECTIONPROPERTY to a NetworkProtocol value
        private static NetworkProtocol ProtocolFromNetTransport(string netTransport)
        {
            switch (netTransport.ToLowerInvariant())
            {
                case "tcp":
                case "http":
                case "ssl":
                    return NetworkProtocol.TcpIp;
                case "named pipe":
                    return NetworkProtocol.NamedPipes;
                case "shared memory":
                    return NetworkProtocol.SharedMemory;
                case "via":
                    return NetworkProtocol.Via;
                default:
                    return NetworkProtocol.NotSpecified;
            }
        }
        public static ServerVersion ParseStringServerVersion(string version)
        {
            var ver = new Version(version.Substring(0, 10));
            return new ServerVersion(ver.Major, ver.Minor, ver.Build);
        }

        public static ServerVersion ParseMicrosoftVersion(UInt32 version)
        {
            // Parse the UINT32 returned via SELECT @@MICROSOFTVERSION
            // and convert that to ServerVersion struct.
            //
            // First byte is the major version, second one is a minor version and the rest is a revision number.
            //
            return new ServerVersion(
                (int)(version / 0x01000000),
                (int)(version / 0x010000 & 15),
                (int)version & 255);
        }
    }

}
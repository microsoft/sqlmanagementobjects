// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#endif


namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Represents Azure Data Studio saved connections
    /// </summary>
    [DataContract]
    public class AzureDataStudioConnectionStore
    {
        /// <summary>
        /// The set of connection groups
        /// </summary>
        [DataMember(Name = "datasource.connectionGroups")]
        public List<AzureDataStudioConnectionGroup> Groups { get; set; }

        /// <summary>
        /// The set of saved connections
        /// </summary>
        [DataMember(Name = "datasource.connections")]
        public List<AzureDataStudioConnection> Connections { get; set; }

        /// <summary>
        /// Constructs a new AzureDataStudioConnectionStore from the given settings file. If no file is specified, 
        /// it looks in %appdata%\azuredatastudio\user\settings.json. 
        /// </summary>
        /// <param name="settingsFile"></param>
        /// <returns></returns>
        public static AzureDataStudioConnectionStore LoadAzureDataStudioConnections(string settingsFile = null)
        {
            if (string.IsNullOrEmpty(settingsFile))
            {
                settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"azuredatastudio\User\settings.json");
            }
            if (File.Exists(settingsFile))
            {
                try
                {
                    using (var fileStream = new FileStream(settingsFile, FileMode.Open, FileAccess.Read))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(AzureDataStudioConnectionStore),
                            new DataContractJsonSerializerSettings() {UseSimpleDictionaryFormat = true});
                        var store = (AzureDataStudioConnectionStore)serializer.ReadObject(fileStream);
                        store.Groups = store.Groups ?? new List<AzureDataStudioConnectionGroup>();
                        store.Connections = store.Connections ?? new List<AzureDataStudioConnection>();
                        return store;
                    }
                }
                catch (Exception e)
                {
#if STRACE
                    STrace.Trace("AzureDataStudioConnectionStore", "Unable to read Azure Data Studio settings: {0}", e.ToString());
#else
                    System.Diagnostics.Trace.TraceInformation("Unable to read Azure Data Studio settings: {0}", e.ToString());
#endif
                }
            }

            return new AzureDataStudioConnectionStore() { Groups = new List<AzureDataStudioConnectionGroup>(), Connections = new List<AzureDataStudioConnection>() };
        }
    }
}

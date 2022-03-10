// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Represents a SQL server connection stored in Azure Data Studio settings
    /// </summary>
    [DataContract]
    public class AzureDataStudioConnection 
    {
        /// <summary>
        /// ID of the containing group
        /// </summary>
        [DataMember(Name = "groupId")]
        public string GroupId { get; set; }

        /// <summary>
        /// Unique ID of the connection
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Name of the connection
        /// </summary>
        [DataMember(Name = "options")]
        public Dictionary<string, string> Options { get; set; }

        /// <summary>
        /// Provider of the connection
        /// </summary>
        [DataMember(Name = "providerName")]
        public string ProviderName { get; set; }

        /// <summary>
        /// Whether the password is saved in credential manager
        /// </summary>
        [DataMember(Name = "savePassword")]
        public bool SavePassword { get; set; }
    }
}

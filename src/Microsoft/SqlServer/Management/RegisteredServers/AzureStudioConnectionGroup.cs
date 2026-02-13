// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Represents a connection group saved in Azure Data Studio settings
    /// </summary>
    [DataContract]
    public class AzureDataStudioConnectionGroup
    {
        /// <summary>
        /// Color used for the group in the UI
        /// </summary>
        [DataMember(Name = "color")]
        public string Color {get; set;}

        /// <summary>
        /// Description of the group
        /// </summary>
        [DataMember(Name = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Unique ID of the group
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Name of the group
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// ID of the parent group, will be null for the root.
        /// </summary>
        [DataMember(Name = "parentId")]
        public string ParentId { get; set; }
    }
}

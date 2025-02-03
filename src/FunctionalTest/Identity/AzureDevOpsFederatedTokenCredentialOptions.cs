// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
namespace Microsoft.SqlServer.ADO.Identity
{
    /// <summary>
    /// Options for an <see cref="AzureDevOpsFederatedTokenCredential"/>
    /// </summary>
    public class AzureDevOpsFederatedTokenCredentialOptions
    {
        /// <summary>
        /// The client id of the identity the service connection is configured for
        /// </summary>
        public string ClientId { get; set; } = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        /// <summary>
        /// A unique identifier for a single attempt of a single job. The value is unique to the current pipeline.
        /// </summary>
        public string JobId { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_JOBID");

        /// <summary>
        /// A string-based identifier for a single pipeline run.
        /// </summary>
        public string PlanId { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_PLANID");

        /// <summary>
        /// The ID of the service connection we'd like to use
        /// </summary>
        public string ServiceConnectionId { get; set; } = Environment.GetEnvironmentVariable("SERVICE_CONNECTION_ID");

        /// <summary>
        /// The security token used by the running build.
        /// </summary>
        public string SystemAccessToken { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");

        /// <summary>
        /// The URI of the TFS collection or Azure DevOps organization.
        /// </summary>
        public string TeamFoundationCollectionUri { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_TEAMFOUNDATIONCOLLECTIONURI");

        /// <summary>
        /// The ID of the project that this build belongs to.
        /// </summary>
        public string TeamProjectId { get; set; } = Environment.GetEnvironmentVariable("SYSTEM_TEAMPROJECTID");

        /// <summary>
        /// The tenant id of the tenant in which we want a token
        /// </summary>
        public string TenantId { get; set; } = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
    }
}

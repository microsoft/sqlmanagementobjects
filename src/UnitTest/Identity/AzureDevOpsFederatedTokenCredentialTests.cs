// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Identity;
using Microsoft.SqlServer.ADO.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.IdentityUnitTests
{
    [TestClass]
    public class AzureDevOpsFederatedTokenCredentialTests
    {
        private const string ServiceConnectionId = "11111111-1111-1111-1111-111111111111";

        [DataRow("https://dev.azure.com/organization/")]
        [DataRow("https://subdomain.dev.azure.com/organization/")]
        [DataRow("https://organization.visualstudio.com/")]
        [DataRow("https://organization.visualstudio.com:443/")]
        [TestCategory("Unit")]
        [TestMethod]
        public void AzureDevOpsFederatedTokenCredential_Constructor_accepts_secure_Azure_DevOps_endpoints(string endpoint)
        {
#pragma warning disable CS0618 // Testing the deprecated credential's security boundary.
            _ = new AzureDevOpsFederatedTokenCredential(CreateOptions(endpoint, ServiceConnectionId));
#pragma warning restore CS0618
        }

        [DataRow("http://organization.visualstudio.com/")]
        [DataRow("https://organization.visualstudio.com:444/")]
        [DataRow("https://evildev.azure.com/organization/")]
        [DataRow("https://dev.azure.com.evil.example/organization/")]
        [DataRow("https://visualstudio.com/")]
        [DataRow("https://evilvisualstudio.com/")]
        [TestCategory("Unit")]
        [TestMethod]
        public void AzureDevOpsFederatedTokenCredential_Constructor_rejects_insecure_or_unrecognized_endpoints(string endpoint)
        {
#pragma warning disable CS0618 // Testing the deprecated credential's security boundary.
            Assert.ThrowsException<CredentialUnavailableException>(
                () => new AzureDevOpsFederatedTokenCredential(CreateOptions(endpoint, ServiceConnectionId)),
                $"Expected endpoint '{endpoint}' to be rejected before a token could be sent");
#pragma warning restore CS0618
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void AzureDevOpsFederatedTokenCredential_Constructor_rejects_invalid_service_connection_id()
        {
#pragma warning disable CS0618 // Testing the deprecated credential's security boundary.
            Assert.ThrowsException<CredentialUnavailableException>(
                () => new AzureDevOpsFederatedTokenCredential(CreateOptions("https://dev.azure.com/organization/", "not-a-guid&redirect=true")),
                "Service connection identifiers must not permit query-string injection");
#pragma warning restore CS0618
        }

        private static AzureDevOpsFederatedTokenCredentialOptions CreateOptions(string endpoint, string serviceConnectionId) =>
            new AzureDevOpsFederatedTokenCredentialOptions
            {
                ClientId = "22222222-2222-2222-2222-222222222222",
                JobId = "33333333-3333-3333-3333-333333333333",
                PlanId = "44444444-4444-4444-4444-444444444444",
                ServiceConnectionId = serviceConnectionId,
                SystemAccessToken = "test-token",
                TeamFoundationCollectionUri = endpoint,
                TeamProjectId = "55555555-5555-5555-5555-555555555555",
                TenantId = "66666666-6666-6666-6666-666666666666",
            };
    }
}

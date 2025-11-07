// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Security;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.SqlServer.ADO.Identity;
namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Retrieves a decrypted secret from Azure Key Vault or environment using certificate auth or client secret or managed identity
    /// </summary>
    public class AzureKeyVaultHelper : ICredential
    {
        /// <summary>
        /// The Azure application id associated with the service principal
        /// </summary>
        public string AzureApplicationId { get; set; } = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        /// <summary>
        /// The Azure tenant id associated with the service principal
        /// </summary>
        public string AzureTenantId { get; set; } = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        /// <summary>
        /// Set this to the client id of a user assigned managed identity
        /// </summary>
        public string AzureManagedIdentityClientId { get; set; } = Environment.GetEnvironmentVariable("AZURE_IDENTITY_CLIENT_ID");
        /// <summary>
        /// The name of the Azure key vault where test secrets are stored.
        /// </summary>
        public string KeyVaultName { get; set; }

        /// <summary>
        /// The AzureStorageHelper instance used to access the storage account
        /// </summary>
        public AzureStorageHelper StorageHelper { get; set; }
        private static readonly IDictionary<string, SecureString> secretCache = new Dictionary<string, SecureString>();
        private static readonly object syncObj = new object();
        public static readonly string SSMS_TEST_SECRET_PREFIX = "SQLA-SSMS-Test-";

        private SecretClient secretClient = null;

        /// <summary>
        /// Constructs a new AzureKeyVaultHelper that relies on an instance of Azure.Identity.DefaultAzureCredential to access the given vault.
        /// </summary>
        /// <param name="keyVaultName"></param>
        public AzureKeyVaultHelper(string keyVaultName)
        {
            
            KeyVaultName = keyVaultName;
        }

        /// <summary>
        /// Converts the secretName to an AKV resource URL and retrieves its decrypted value
        /// If the value exists as an environment variable, the environment variable value is used.
        /// Note that / characters in the secretName are replaced by - and _ by a space before the lookup.
        /// </summary>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public string GetDecryptedSecret(string secretName)
        {
            var secret = string.Empty;
            var lookupName = secretName.Replace('/', '-').Replace("_", "");
            lock (syncObj)
            {
                if (secretCache.ContainsKey(secretName))
                {
                    secret = secretCache[secretName].SecureStringToString();
                }
            }
            if (string.IsNullOrEmpty(secret))
            {
                TraceHelper.TraceInformation("Looking for secret {0} using name {1}. Starting with environment variables.", secretName, lookupName);
                secret = Environment.GetEnvironmentVariable(lookupName);
            }
            if (string.IsNullOrEmpty(secret))
            {
                // It's ok if multiple threads race to construct this secretClient instance
                if (secretClient == null)
                {
                    var credential = GetCredential();
                    secretClient = new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net"), credential);
                }
                var secretIdentifier = $"https://{KeyVaultName}.vault.azure.net/secrets/{lookupName}";
                TraceHelper.TraceInformation("Secret {0} not set as environment variable. Looking in AKV for {1}.", secretName, secretIdentifier);
                try
                {
                    secret = secretClient.GetSecret(lookupName).Value.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Got Exception fetching secret. Type:{0}, Inner:{1}, Outer:{2}", e.GetType(), e.InnerException, e);
                    throw;
                }
                // Note we aren't bothering to cache secrets we found from GetEnvironmentVariable since that API is already fast
                lock (syncObj)
                {
                    secretCache[secretName] = secret.StringToSecureString();
                }
            }
            return secret;
        }

        /// <summary>
        /// Returns a TokenCredential that implements Managed Identity, DefaultAzureCredential, and AzurePipelinesCredential in that order.
        /// </summary>
        /// <returns></returns>
        public Azure.Core.TokenCredential GetCredential()
        {
            TraceHelper.TraceInformation($"Getting credential for Azure in tenant {AzureTenantId}");
            // prefer managed identity then local user on dev machine over the certificate
            var credentials = new List<Azure.Core.TokenCredential>() { new ManagedIdentityCredential(AzureManagedIdentityClientId), new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true, TenantId = AzureTenantId }) };
            var options = new AzureDevOpsFederatedTokenCredentialOptions() { TenantId = AzureTenantId, ClientId = AzureApplicationId };
            if (options.ServiceConnectionId != null)
            {
                TraceHelper.TraceInformation($"Adding AzurePipelinesCredential for tenant id {options.TenantId} using service connection {options.ServiceConnectionId}");
                credentials.Insert(0, new AzurePipelinesCredential(options.TenantId, options.ClientId, options.ServiceConnectionId, options.SystemAccessToken));
            }
            return new ChainedTokenCredential(credentials.ToArray());
        }

        /// <summary>
        /// Returns the account access key for the given storage account resource id.
        /// </summary>
        /// <param name="storageAccountResourceId"></param>
        /// <returns></returns>
        public string GetStorageAccountAccessKey(string storageAccountResourceId)
        {
            TraceHelper.TraceInformation($"Fetching storage access key for {storageAccountResourceId}");
            return new AzureStorageHelper(storageAccountResourceId, this).GetStorageAccountAccessKey(storageAccountResourceId);
        }
    }

    public interface ICredential
    {
        Azure.Core.TokenCredential GetCredential();
    }
}
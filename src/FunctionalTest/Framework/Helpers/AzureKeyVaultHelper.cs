// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Retrieves a decrypted secret from Azure Key Vault or environment using certificate auth or client secret or managed identity
    /// </summary>
    public class AzureKeyVaultHelper
    {
        /// <summary>
        /// The set of certificate thumbprints associated with the service principal.
        /// If this collection is non-empty, AzureApplicationId and AzureTenantId must also be set to valid values.
        /// Set these properties to use certificate-based authentication without relying on environment variables to specify the certificate.
        /// </summary>
        public IEnumerable<string> CertificateThumbprints { get; set; }
        /// <summary>
        /// The Azure application id associated with the service principal
        /// </summary>
        public string AzureApplicationId { get; set; }
        /// <summary>
        /// The Azure tenant id associated with the service principal
        /// </summary>
        public string AzureTenantId { get; set; }
        /// <summary>
        /// The name of the Azure key vault where test secrets are stored.
        /// </summary>
        public string KeyVaultName { get; private set; }
        
        private static readonly IDictionary<string,SecureString> secretCache = new Dictionary<string, SecureString>();
        private static readonly object syncObj = new object();
        public static readonly string SSMS_TEST_SECRET_PREFIX = "SQLA-SSMS-Test-";

        /// <summary>
        /// Constructs a new AzureKeyVaultHelper that relies on an instance of Azure.Identity.DefaultAzureCredential to access the given vault.
        /// </summary>
        /// <param name="keyVaultName"></param>
        public AzureKeyVaultHelper(string keyVaultName)
        {
            
            KeyVaultName = keyVaultName;
            CertificateThumbprints = Enumerable.Empty<string>();
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
                Azure.Core.TokenCredential credential = new DefaultAzureCredential();
                foreach (var thumbprint in CertificateThumbprints ?? Enumerable.Empty<string>())
                {
                    var certificate = FindCertificate(thumbprint);
                    if (certificate != null)
                    {
                        credential = new ClientCertificateCredential(AzureTenantId, AzureApplicationId, certificate);
                    }
                    break;
                }

                var secretIdentifier = $"https://{KeyVaultName}.vault.azure.net/secrets/{lookupName}";
                TraceHelper.TraceInformation("Secret {0} not set as environment variable. Looking in AKV for {1}.", secretName, secretIdentifier);
                try
                {
                    var secretClient = new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net"), credential);
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

        private static X509Certificate2 FindCertificate(string thumbprint)
        {
            X509Certificate2 certificate = null;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                try
                {
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certificateCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
                    if (certificateCollection.Count == 0)
                    {
                        TraceHelper.TraceInformation("Couldn't find Smo cert {0} in local machine. Looking in current user", thumbprint);
                        var userStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        userStore.Open(OpenFlags.ReadOnly);
                        try
                        {
                            certificateCollection = userStore.Certificates.Find(X509FindType.FindByThumbprint,
                                thumbprint, validOnly: false);
                        }
                        finally
                        {
                            userStore.Close();
                        }
                    }
                    if (certificateCollection.Count != 0)
                    {
                        TraceHelper.TraceInformation("Found cert {0}", thumbprint);
                        certificate = certificateCollection[0];
                    }
                }
                finally
                {
                    store.Close();
                }
            }
            return certificate;
        }
        
    }
}
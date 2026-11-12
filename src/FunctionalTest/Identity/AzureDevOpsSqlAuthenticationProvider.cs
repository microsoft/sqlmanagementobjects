// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace Microsoft.SqlServer.ADO.Identity
{
    /// <summary>
    /// Authentication provider for service principal authentication or default authentication that uses Azure Devops federated token provider, certificate provider, and default azure credential
    /// </summary>
    public class AzureDevOpsSqlAuthenticationProvider : SqlAuthenticationProvider
    {
        private readonly ConcurrentDictionary<string, TokenData> _credentials = new ConcurrentDictionary<string, TokenData>();
        private static readonly string s_defaultScopeSuffix = "/.default";

        /// <summary>
        /// Acquires a token for the given parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
        {
            var authorityComponents = parameters.Authority.Split('/');
            var tenant = authorityComponents.Last();
            var tokenData = _credentials.GetOrAdd(parameters.UserId + tenant, u => new TokenData(CredentialFromUserId(tenant, parameters.UserId, parameters.Password, parameters.AuthenticationMethod)));
            var accessToken = await tokenData.AcquireTokenAsync(parameters, tenant);
            Trace.TraceInformation($"{DateTime.Now:o}:AzureDevOpsSqlAuthenticationProvider: Returning token for '{parameters.UserId}@{tenant}'. Expires: {accessToken.ExpiresOn.ToLocalTime():o}");
            return new SqlAuthenticationToken(accessToken.Token, accessToken.ExpiresOn);
        }

        /// <summary>
        /// Returns true if this provider supports the given authentication method
        /// </summary>
        /// <param name="authenticationMethod"></param>
        /// <returns></returns>
        public override bool IsSupported(SqlAuthenticationMethod authenticationMethod) =>
            authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryServicePrincipal || authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryDefault || authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity;


        private static TokenCredential CredentialFromUserId(string tenantId, string userId, string password, SqlAuthenticationMethod authenticationMethod)
        {
            Trace.TraceInformation($"Getting TokenCredential for auth method {authenticationMethod}");
            if (authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryManagedIdentity)
            {
                return new ManagedIdentityCredential(userId);
            }
            var credentials = new List<TokenCredential>();
            // if password is provided, assume it's a cert thumbprint
            if (!string.IsNullOrEmpty(password))
            {
                var certificate = FindCertificate(password);
                if (certificate != null)
                {
                    Trace.TraceInformation($"Adding ClientCertificateCredential for thumbprint {password.Substring(0, 10)}");
                    credentials.Add(new ClientCertificateCredential(tenantId, userId, certificate));
                }
            }
            // if using ActiveDirectoryDefault, the AZURE_CLIENT_ID variable has to be set to use service principal authentication in Azure devops
            var options = new AzureDevOpsFederatedTokenCredentialOptions() { TenantId = tenantId };
            if (!string.IsNullOrEmpty(userId))
            {
                options.ClientId = userId;
            }
            if (options.ServiceConnectionId != null)
            {
                credentials.Add(new AzurePipelinesCredential(options.TenantId, options.ClientId, options.ServiceConnectionId, options.SystemAccessToken));
                Trace.TraceInformation($"Adding AzurePipelinesCredential for client id {options.ClientId ?? "<NULL>"}");
            }
            if (authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryDefault)
            {
                Trace.TraceInformation($"Adding DefaultAzureCredential for tenant {tenantId}");
                var defOptions = new DefaultAzureCredentialOptions() { ExcludeManagedIdentityCredential = true, TenantId = tenantId };
                defOptions.AdditionallyAllowedTenants.Add(tenantId);
                if (string.IsNullOrEmpty(userId))
                {
                    userId = Environment.GetEnvironmentVariable("AZURE_IDENTITY_CLIENT_ID");
                }
                Trace.TraceInformation($"Adding ManagedIdentityCredential for client {userId ?? "<NULL>"}");
                if (options.ServiceConnectionId == null)
                {
                    credentials.Insert(0, new ManagedIdentityCredential(userId));
                }
                credentials.Insert(0, new DefaultAzureCredential(defOptions));
                return new ChainedTokenCredential(credentials.ToArray());
            }
            return credentials.Any() ? (TokenCredential)new ChainedTokenCredential(credentials.ToArray()) : new EnvironmentCredential();
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

        private class TokenData
        {
            public TokenCredential Credential { get; }
            public AccessToken AccessToken { get; private set; }
                = new AccessToken(string.Empty, DateTimeOffset.MinValue.AddMinutes(5)); // 5 minutes padding for expiration check
            private object SyncObj { get; } = new object();
            public TokenData(TokenCredential credential)
            {
                Credential = credential;
            }

            private AccessToken SetToken(AccessToken accessToken)
            {
                lock (SyncObj)
                {
                    tokenSource?.Dispose();
                    tokenSource = null;
                    return AccessToken = accessToken;
                }
            }

            private Task<AccessToken> currentTask;
            private CancellationTokenSource tokenSource = null;
            public Task<AccessToken> AcquireTokenAsync(SqlAuthenticationParameters parameters, string tenant)
            {
                lock (SyncObj)
                {
                    var expired = AccessToken.ExpiresOn.AddMinutes(-1) < DateTime.UtcNow;
                    if (!expired)
                    {
                        var accessToken = AccessToken;
                        currentTask = Task.FromResult(accessToken);
                    }
                    else if (tokenSource == null) // not already waiting on a task
                    {
                        Trace.TraceInformation($"{DateTime.Now:o}:AzureDevOpsSqlAuthenticationProvider: Acquiring new token for '{parameters.UserId}@{tenant}'");
                        var scope = parameters.Resource.EndsWith(s_defaultScopeSuffix, StringComparison.Ordinal) ? parameters.Resource : parameters.Resource + s_defaultScopeSuffix;
                        var scopes = new string[] { scope };
                        tokenSource = new CancellationTokenSource();
                        currentTask = Credential.GetTokenAsync(new TokenRequestContext(scopes, tenantId: tenant), tokenSource.Token).AsTask().ContinueWith(t => SetToken(t.Result));
                    }

                    return currentTask;
                }
            }
        }
    }
}

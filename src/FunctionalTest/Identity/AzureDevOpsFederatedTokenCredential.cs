// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Client;

namespace Microsoft.SqlServer.ADO.Identity
{
    /// <summary>
    /// A <see cref="TokenCredential"/> for use in ADO pipelines with AzureRM service connections
    /// set up for workload identity federation.
    /// </summary>
    public class AzureDevOpsFederatedTokenCredential : TokenCredential
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _systemAccessToken;
        private readonly Uri _oidcRequestUri;
        private readonly IConfidentialClientApplication _confidentialClientApplication;

        /// <summary>
        /// Construct a new <see cref="AzureDevOpsFederatedTokenCredential"/>
        /// </summary>
        /// <param name="options">The options for the auth scenario</param>
        /// <exception cref="InvalidOperationException">If environment variables expected to be present in pipelines aren't available</exception>
        public AzureDevOpsFederatedTokenCredential(AzureDevOpsFederatedTokenCredentialOptions options = null)
        {
            options = options ?? new AzureDevOpsFederatedTokenCredentialOptions();

            _systemAccessToken = options.SystemAccessToken;
            _oidcRequestUri = BuildOidcRequestUri(options);
            Trace.TraceInformation("Oidc Uri " + _oidcRequestUri?.ToString() ?? "NONE");
            _confidentialClientApplication = BuildConfidentialClientApplication(options);
        }

        /// <summary>
        /// Get an access token synchronously. Prefer <see cref="GetTokenAsync(TokenRequestContext, CancellationToken)"/>
        /// </summary>
        /// <param name="requestContext">The context for the token request</param>
        /// <param name="cancellationToken">A token to allow cancellation of the internal async task</param>
        /// <returns>The retrieved <see cref="AccessToken"/></returns>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();

        /// <summary>
        /// Get an access token asynchronously.
        /// </summary>
        /// <param name="requestContext">The context for the token request</param>
        /// <param name="cancellationToken">A token to allow cancellation of the internal async task</param>
        /// <returns>The retrieved <see cref="AccessToken"/></returns>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            if (_confidentialClientApplication is null)
            {
                throw new CredentialUnavailableException("We were not provided with all the necessary information to construct a ConfidentialClientApplication!");
            }

            var builder = _confidentialClientApplication
                .AcquireTokenForClient(requestContext.Scopes);

            if (!string.IsNullOrEmpty(requestContext.TenantId))
            {
                builder = builder.WithTenantId(requestContext.TenantId);
            }

            if (!string.IsNullOrEmpty(requestContext.Claims))
            {
                builder = builder.WithClaims(requestContext.Claims);
            }

            var authResult = await builder
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return new AccessToken(authResult.AccessToken, authResult.ExpiresOn);
        }

        private static Uri BuildOidcRequestUri(AzureDevOpsFederatedTokenCredentialOptions options)
        {
            Trace.TraceInformation("Creating Oidc request Uri");
            if (options.TeamFoundationCollectionUri is null || options.TeamProjectId is null || options.PlanId is null || options.JobId is null || options.ServiceConnectionId is null)
            {
                return null;
            }

            var baseUri = new Uri(options.TeamFoundationCollectionUri);
            var teamProjectId = Uri.EscapeDataString(options.TeamProjectId);
            var planId = Uri.EscapeDataString(options.PlanId);
            var jobId = Uri.EscapeDataString(options.JobId);

            return new Uri(FormattableString.Invariant(
                $"{baseUri}{teamProjectId}/_apis/distributedtask/hubs/build/plans/{planId}/jobs/{jobId}/oidctoken?api-version=7.1-preview.1&serviceConnectionId={options.ServiceConnectionId}"));
        }

        private IConfidentialClientApplication BuildConfidentialClientApplication(AzureDevOpsFederatedTokenCredentialOptions options)
        {
            if (options.ClientId is null || options.TenantId is null || options.ServiceConnectionId is null || options.TeamProjectId is null)
            {
                return null;
            }
            Trace.TraceInformation($"Building confidential app for client {options.ClientId}@{options.TenantId}");
            return ConfidentialClientApplicationBuilder
                .Create(options.ClientId)
                .WithTenantId(options.TenantId)
                .WithClientAssertion(GetClientAssertionAsync)
                .Build();
        }

        private static bool IsValidAzureDevOpsHost(Uri baseUri) =>  baseUri.DnsSafeHost.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase) || baseUri.DnsSafeHost.EndsWith("dev.azure.com", StringComparison.OrdinalIgnoreCase);


        private async Task<string> GetClientAssertionAsync(AssertionRequestOptions options)
        {
            if (_oidcRequestUri is null || string.IsNullOrEmpty(_systemAccessToken))
            {
                throw new CredentialUnavailableException("We were not provided with all the necessary information to construct an OIDC request URI!");
            }

            if (!IsValidAzureDevOpsHost(_oidcRequestUri))
            {
                throw new CredentialUnavailableException(FormattableString.Invariant(
                    $"We were provided with a host ({_oidcRequestUri.DnsSafeHost}) that we do not believe is a valid ADO host. Rejecting due to security concerns."));
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, _oidcRequestUri)
            {
                Headers =
                        {
                            Authorization = new AuthenticationHeaderValue("Bearer", _systemAccessToken),
                        },
                Content = new StringContent("{}")
                {
                    Headers =
                            {
                                ContentType = new MediaTypeHeaderValue("application/json"),
                            },
                },
            })
            {

                using (var response = await _httpClient.SendAsync(request, options.CancellationToken).ConfigureAwait(false))
                {
                    var headers = string.Join(",", response.Headers.Select(h => $"<{h.Key}: {string.Join(",", h.Value)}>"));
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Trace.TraceInformation($"Got response from OidcToken: {response.StatusCode}:{error}:{headers}");
                    }
                    _ = response.EnsureSuccessStatusCode();

                    using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var responseObject = await JsonSerializer.DeserializeAsync<AdoOidcResponse>(responseStream, cancellationToken: options.CancellationToken).ConfigureAwait(false);
                        return responseObject?.OidcToken is null
                            ? throw new CredentialUnavailableException("Failed to parse the response from ADO OIDC endpoint!")
                            : responseObject.OidcToken;
                    }
                }
            }
        }
    }
}

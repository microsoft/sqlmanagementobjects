using System.Text.Json.Serialization;

namespace Microsoft.SqlServer.ADO.Identity
{
    /// <summary>
    /// The response returned by the ADO OIDC endpoint
    /// </summary>
    internal class AdoOidcResponse
    {
        /// <summary>
        /// An idToken that can be used to authenticate as the service connection.
        /// </summary>
        [JsonPropertyName("oidcToken")]
        public string OidcToken { get; set; }
    }
}

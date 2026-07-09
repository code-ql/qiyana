using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace qiyana.Models;

public class EntitlementsToken
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("entitlements")]
    public List<object> Entitlements { get; set; } = [];

    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
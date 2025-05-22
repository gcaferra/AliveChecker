using System.Text.Json.Serialization;

namespace AliveChecker.Application.Auth.Models;

public record Token
{
    public static Token Empty = new Token();

    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;
}
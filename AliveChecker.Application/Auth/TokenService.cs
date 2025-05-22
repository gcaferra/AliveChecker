using System.Security.Claims;
using System.Text.Json;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;

namespace AliveChecker.Application.Auth;

public interface ITokenService
{
    string GetAuditToken(Guid idToken);
    string GetClientAssertion(Guid idToken, string body);
    string GetSignature(string body);
}

public class TokenService(ClientConfiguration configuration,ITokenCreator tokenCreator, IDateProvider dateProvider, IHashService hashService, ILogger<TokenService> logger) : ITokenService
{
    ClientConfiguration Configuration { get; } = configuration;
    ITokenCreator TokenCreator { get; } = tokenCreator;

    const int TokenExpirationMinutes = 5;

    public string GetAuditToken(Guid idToken)
    {
        var currentTime = dateProvider.UtcNow;
        var expirationTime = currentTime.AddMinutes(TokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new ( "jti", idToken.ToString() ),
            new ( "purposeId", Configuration.PurposeId ),
            new ( "dnonce", GetDNonce()),
            new ( "userID", Configuration.UserId),
            new ( "LoA", "LOA3" ),
            new ( "aud", Configuration.SignatureAudience ),
            new ("userLocation", Environment.MachineName),
            new ( "iss", Configuration.ClientId ),
            new ( "sub", Configuration.ClientId ),
            new ( "iat", currentTime.ToUnixTimeSeconds().ToString() , ClaimValueTypes.Integer64 ),
            new ( "exp", expirationTime.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64 ),
            // new(JwtRegisteredClaimNames.Nbf, currentTime.AddSeconds(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };
        return TokenCreator.CreateJwtToken(claims);
    }

    public string GetClientAssertion(Guid idToken, string body)
    {
        var currentTime = dateProvider.UtcNow;
        var auditHash = hashService.ToHexString(body);
        logger.LogDebug("AuditHash: {hash}", auditHash);

        var expireIn = currentTime.AddMinutes(TokenExpirationMinutes);
        
        Dictionary<string, string> digest = new Dictionary<string, string>
        {
            { "alg", "SHA256" },
            { "value", auditHash }
        };

        var claims = new List<Claim>()
        {
            new(JwtRegisteredClaimNames.Jti, idToken.ToString()),
            new(JwtRegisteredClaimNames.Sub, Configuration.ClientId),
            new(JwtRegisteredClaimNames.Aud, Configuration.Audience),
            new(JwtRegisteredClaimNames.Iss, Configuration.ClientId),
            new("purposeId", Configuration.PurposeId),
            new(JwtRegisteredClaimNames.Iat, currentTime.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Exp, expireIn.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            // new(JwtRegisteredClaimNames.Nbf, currentTime.AddSeconds(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("digest", JsonSerializer.Serialize(digest), JsonClaimValueTypes.Json)
        };
        return TokenCreator.CreateJwtToken(claims);
    }

    public string GetSignature(string body)
    {
        var currentTime = dateProvider.UtcNow;
        var digest = hashService.ToBase64String(body);
        var expire = currentTime.AddMinutes(TokenExpirationMinutes);
    
        var signedHeaders = new List<Dictionary<string, string>>
        {
            new() { { "digest", $"SHA-256={digest}" } },
            new() { { "content-type", "application/json" } }
        };
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, Configuration.ClientId),
            new(JwtRegisteredClaimNames.Aud, Configuration.SignatureAudience),
            new(JwtRegisteredClaimNames.Iss, Configuration.ClientId),
            new(JwtRegisteredClaimNames.Iat, currentTime.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Exp, expire.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            // new(JwtRegisteredClaimNames.Nbf, currentTime.AddSeconds(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("signed_headers", JsonSerializer.Serialize(signedHeaders), JsonClaimValueTypes.Json)
        };

        return TokenCreator.CreateJwtToken(claims);

    }

    static string GetDNonce()
    {
        var random =  new Random(Environment.TickCount);
        return GenerateRandomString(random, 13);
    }
    
    static string GenerateRandomString(Random random, int length)
    {
        // Generate random digits and concatenate them into a string
        var randomString = string.Empty;
        for (int i = 0; i < length; i++)
        {
            // Append a random digit (0-9) to the string
            randomString += random.Next(10).ToString();
        }

        return randomString;
    }
}
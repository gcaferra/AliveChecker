using System.Net.Http.Json;
using AliveChecker.Application.Auth.Models;
using AliveChecker.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace AliveChecker.Application.Auth;

public interface IAuthService
{
    Task<AuthenticationResult> AuthenticateAssertion(Guid idToken);
}

public class AuthService(
    ClientConfiguration configuration,
    ILogger<AuthService> logger,
    HttpClient client,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthenticationResult> AuthenticateAssertion(Guid idToken)
    {
        var auditToken = tokenService.GetAuditToken(idToken);
        logger.LogDebug("AuditToken: {AuditToken}", auditToken);

        var clientAssertion = tokenService.GetClientAssertion(idToken, auditToken);
        logger.LogDebug("ClientAssertion: {ClientAssertions}", clientAssertion);


        var authenticationUrl = configuration.AuthenticationUrl;

        using var request = new FormUrlEncodedContent(new KeyValuePair<string, string>[]
        {
            new("client_id", configuration.ClientId),
            new("client_assertion", clientAssertion),
            new("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new("grant_type", "client_credentials"),
        });

        var response = await client.PostAsync(authenticationUrl, request);
        if (response.IsSuccessStatusCode)
        {
            logger.LogDebug("Token authenticated");
            var token = await response.Content.ReadFromJsonAsync<Token>();
            return  new AuthenticationResult(token, auditToken, true);
        }

        logger.LogWarning("Token not created {@response}", response);
        return new AuthenticationResult(Token.Empty, string.Empty, false);
    }

}
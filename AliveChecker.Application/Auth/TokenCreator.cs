using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AliveChecker.Application.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AliveChecker.Application.Auth;

public interface ITokenCreator
{
    string CreateJwtToken(List<Claim> claims);
}

public class TokenCreator(ClientConfiguration clientConfiguration) : ITokenCreator
{
    ClientConfiguration Configuration { get; } = clientConfiguration;

    public string CreateJwtToken(List<Claim> claims)
    {
        var rsa = RSA.Create();
        var privateKey = File.ReadAllText(Configuration.PrivateKey);
        rsa.ImportFromPem(privateKey);

        var tokenHandler = new JwtSecurityTokenHandler();
        var rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = Configuration.KeyId

        };
        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256, SecurityAlgorithms.Sha256);

        var jwtSecurityToken = new JwtSecurityToken(
            claims: claims,
            signingCredentials: signingCredentials
        );
        
        var joseToken = tokenHandler.WriteToken(jwtSecurityToken);
        return joseToken;
    }

}
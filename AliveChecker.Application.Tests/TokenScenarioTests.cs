using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AliveChecker.Application.Auth;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AliveChecker.Application.Tests;

public class TokenScenarioTests
{
    readonly ITokenService _sut;
    readonly ClientConfiguration _configuration;
    readonly ITokenCreator _tokenCreator;
    readonly IDateProvider _dateProvider;
    readonly IHashService _hashService;

    public TokenScenarioTests()
    {
        _configuration = new()
        {
            KeyId = "SkLpMIiLjUxasmi1b8lvrv3U8TRMWp61CfYUbmPBXsw",
            ClientId = "3eb7ceb9-0a22-4eda-97d0-ef71ccb2cf44",
            Audience = "auth.uat.interop.pagopa.it/client-assertion",
            ServiceUrl =
                "https://modipa-val.anpr.interno.it/govway/rest/in/MinInternoPortaANPR/C019-servizioAccertamentoEsistenzaVita/v1",
            PurposeId = "0136c028-c4d0-4c5e-89ca-6630987564b2",
            PrivateKey = "testkey.pem",
            CsvFilePath = "cvsFilePath",
            AuthenticationUrl =
                "https://modipa-val.anpr.interno.it/govway/rest/in/MinInternoPortaANPR-PDND/C019-servizioAccertamentoEsistenzaVita/v1/anpr-service-e002",
            UserId = "UserId"
        };

        _tokenCreator = Substitute.For<ITokenCreator>();
        _dateProvider = Substitute.For<IDateProvider>();
        _hashService = Substitute.For<IHashService>();
        
        _sut = new TokenService(_configuration, _tokenCreator, _dateProvider, _hashService, NullLogger<TokenService>.Instance); 
    }
    
    [Fact]
    public void GetAuditToken_calls_CreateJwtToken()
    {
        // Arrange
        var idToken = Guid.NewGuid();

        // Act
        _sut.GetAuditToken(idToken);

        // Assert
        _tokenCreator.Received().CreateJwtToken(Arg.Any<List<Claim>>());
    }

    [Fact]
    public void GetAuditToken_returns_a_token()
    {
        // Arrange
        var idToken = Guid.NewGuid();
        const string expected = "thiIsAValidToken";
        _tokenCreator.CreateJwtToken(Arg.Any<List<Claim>>())
            .Returns(expected);
        
        // Act
        var actual = _sut.GetAuditToken(idToken);
        
        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void GetAuditToken_returns_a_valid_token_with_give_claims()
    {
        // Arrange
        var idToken = Guid.NewGuid();
        var tokenService = new TokenService(_configuration, new TokenCreator(_configuration), _dateProvider, _hashService, NullLogger<TokenService>.Instance);
        
        // Act
        var token = tokenService.GetAuditToken(idToken);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var actual = tokenHandler.ReadJwtToken(token);
        // Assert
        actual.Claims.Should().HaveCount(12);
    }
    
    [Fact]
    public void GetClientAssertion_calls_CreateJwtToken()
    {
        // Arrange
        var idToken = Guid.NewGuid();
        const string auditHash = "auditHash";
        const string expected = "thiIsAValidToken";
        _tokenCreator.CreateJwtToken(Arg.Any<List<Claim>>())
            .Returns(expected);
        
        // Act
        _sut.GetClientAssertion(idToken, auditHash);
        
        // Assert
        _tokenCreator.Received().CreateJwtToken(Arg.Any<List<Claim>>());
    }
    
    
    [Fact]
    public void GetTokenRequest_returns_a_valid_token_with_give_claims()
    {
        // Arrange
        var idToken = Guid.NewGuid();
        const string auditHash = "auditHash";
        var tokenService = new TokenService(_configuration, new TokenCreator(_configuration), _dateProvider, _hashService, NullLogger<TokenService>.Instance);
        
        // Act
        var token = tokenService.GetClientAssertion(idToken, auditHash);
        
        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var actual = tokenHandler.ReadJwtToken(token);

        actual.Claims.Should().HaveCount(9);

    }
    
    [Fact]
    public void GetSignature_calls_Authorization_Service()
    {
        // Arrange
        const string digest = "digest";
        
        // Act
        _sut.GetSignature(digest);
        
        // Assert
        _tokenCreator.Received().CreateJwtToken(Arg.Any<List<Claim>>());       
    }


}
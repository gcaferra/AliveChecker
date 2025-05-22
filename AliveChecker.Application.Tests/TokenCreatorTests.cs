using System.Security.Claims;
using AliveChecker.Application.Auth;
using AliveChecker.Application.Configuration;
using FluentAssertions;

namespace AliveChecker.Application.Tests;

public class TokenCreatorTests
{
    readonly ClientConfiguration _clientConfiguration = new()
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

    [Fact]
    public void the_Token_is_created_and_signed_with_PrivateKey()
    {
        // Arrange
        var tokenCreator = new TokenCreator(_clientConfiguration);
        
        // Act
        var actual = tokenCreator.CreateJwtToken(new List<Claim>());
        
        // Assert
        actual.Should().Be("eyJhbGciOiJSUzI1NiIsImtpZCI6IlNrTHBNSWlMalV4YXNtaTFiOGx2cnYzVThUUk1XcDYxQ2ZZVWJtUEJYc3ciLCJ0eXAiOiJKV1QifQ.e30.qIH1zNWzmiIDtJ3F0M_mBae8Yg576Jtyieg8OKb8HQr2AHy3AUA0H58G3LUyQ2Hty2xu7iqgIpsvRUeM78Evr8atO2CPBLKgG6sK6nGMZEgUGuUTOAWw0EaCbgKK2vYR9_fb-t6cqo9RbifEI19thKk68pujbLoxD5wPzZ93KPxkYUuJDWu5bySj2BEGXHNP6bSANThQAKHRr0VDQBKeat9E8GuyzrBAIwlbcaXbBG3NQ6e0mpGovYPkLC4J1OhsasboTsk8gKB4dumNEmHJO-tilpeb_QCeDptvC06o_7P47QNj9tyJZVhLiGR5m0WpMDUzj4-gUu_MCrfhLEeNwg");
    }
}
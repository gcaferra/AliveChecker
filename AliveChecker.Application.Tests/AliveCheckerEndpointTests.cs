using System.Net;
using AliveChecker.Application.Auth.Models;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Endpoints;
using AliveChecker.Application.Endpoints.Models;
using AliveChecker.Application.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace AliveChecker.Application.Tests;

public class AliveCheckerEndpointTests
{
    readonly MockHttpMessageHandler _mock;
    readonly HttpClient _client;
    readonly IHashService _hashService;
    readonly IDateProvider _dateProvider;
    readonly string _serviceUrl = "http://localhost/serviceurl";
    readonly AliveCheckerEndpoint _sut;
    readonly ClientConfiguration _clientConfiguration;

    public AliveCheckerEndpointTests()
    {
        _mock = new MockHttpMessageHandler();
        _client = _mock.ToHttpClient();
        _hashService = Substitute.For<IHashService>();
        _dateProvider = Substitute.For<IDateProvider>();

        _dateProvider.UtcNow.Returns(DateTimeOffset.UtcNow);


        _clientConfiguration = new ClientConfiguration() { ServiceUrl = _serviceUrl};
        _sut = new AliveCheckerEndpoint(_clientConfiguration, NullLogger<AliveCheckerEndpoint>.Instance, _client,
            _hashService, _dateProvider);
    }

    [Fact]
    async Task FetchPersonData_Receive_NotFound_if_the_TaxId_is_NotFound()
    {
        _mock.When(_serviceUrl)
            .Respond(HttpStatusCode.NotFound,"application/json","{\"listaErrori\":[{\"codiceErroreAnomalia\":\"EN122\",\"testoErroreAnomalia\":\"La richiesta effettuata non produce alcun risultato\",\"tipoErroreAnomalia\":\"E\"}],\"idOperazioneANPR\":\"1673581176\"}");

        var response = await _sut.FetchPersonData(new Token(), "audit", "signature", "bodyContent");

        response.Should().BeOfType<NotFoundResponse>().Which.StatusDescription.Should().Be("NotFound");
    }

    [Fact]
    async Task FetchPersonData_Receive_NotFound_After_the_Token_is_expired()
    {
        _mock.When(_serviceUrl)
            .Respond(HttpStatusCode.NotFound,"application/json","{ \"type\": \"https://govway.org/handling-errors/404/NotFound.html\", \"title\": \"NotFound\", \"status\": 404, \"detail\": \"Unknown API Request\", \"X-Global-Transaction-ID\": \"7dbb991665eac9c30dfe8bed\" } ");

        var response = await _sut.FetchPersonData(new Token(), "audit", "signature", "bodyContent");

        var actual = response as NotFoundByTokenExpiredResponse;

        actual.Should().NotBeNull();
        actual!.StatusDescription.Should().Be("NotFound/TokenExpired");

    }

    [Fact]
    async Task FetchPersonData_Receive_Unauthorized_when_the_Token_is_expired()
    {
        _mock.When(_serviceUrl)
            .Respond(HttpStatusCode.Unauthorized,"application/json","{\"type\":\"https://govway.org/handling-errors/401/TokenExpired.html\",\"title\":\"TokenExpired\",\"status\":401,\"detail\":\"Expired token\",\"govway_id\":\"452b2364-dd24-11ee-b2b4-005056ae6555\"}");

        var response = await _sut.FetchPersonData(new Token(), "audit", "signature", "bodyContent");

        var actual = response as UnauthorizedResponse;

        actual.Should().NotBeNull();
        actual!.StatusDescription.Should().Be(HttpStatusCode.Unauthorized.ToString());

    }
}
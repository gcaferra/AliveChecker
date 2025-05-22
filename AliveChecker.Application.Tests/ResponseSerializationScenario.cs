using System.Text.Json;
using AliveChecker.Application.Endpoints.Models;
using FluentAssertions;

namespace AliveChecker.Application.Tests;

public class ResponseSerializationScenario
{
    [Fact]
    void ErrorResponse_is_mapped_an_object()
    {
        var json =
            "{\"listaErrori\":[{\"codiceErroreAnomalia\":\"EN122\",\"testoErroreAnomalia\":\"La richiesta effettuata non produce alcun risultato\",\"tipoErroreAnomalia\":\"E\"}],\"idOperazioneANPR\":\"1661088945\"}";

        var response = JsonSerializer.Deserialize<ErrorResponse>(json);

        response.Should().NotBeNull();
        response!.IdOperazioneAnpr.Should().Be("1661088945");
    }
}
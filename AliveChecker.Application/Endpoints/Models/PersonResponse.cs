using System.Text.Json.Serialization;

namespace AliveChecker.Application.Endpoints.Models;

public record ServiceResponse(string FullResponse = "");

public record PersonResponse: ServiceResponse
{
    [JsonPropertyName("listaSoggetti")]
    public ListaSoggetti ListaSoggetti { get; set; }
    [JsonPropertyName("idOperazioneANPR")]
    public string IdOperazioneAnpr { get; set; }
}
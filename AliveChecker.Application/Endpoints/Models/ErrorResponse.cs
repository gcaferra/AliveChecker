using System.Text.Json.Serialization;

namespace AliveChecker.Application.Endpoints.Models;

public class ErrorResponse
{
    [JsonPropertyName("listaErrori")]
    public Error[] Errors { get; set; }
    [JsonPropertyName("idOperazioneANPR")]
    public string IdOperazioneAnpr { get; set; }
}

public class Error
{
    [JsonPropertyName("codiceErroreAnomalia")]
    public string CodiceErroreAnomalia { get; set; }
    [JsonPropertyName("testoErroreAnomalia")]
    public string TestoErroreAnomalia { get; set; }
    [JsonPropertyName("tipoErroreAnomalia")]
    public string TipoErroreAnomalia { get; set; }
}
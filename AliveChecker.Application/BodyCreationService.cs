using System.Text.Json;

namespace AliveChecker.Application;

public interface IBodyCreationService
{
    string CreateBody(string taxId, int operationId, DateTimeOffset currentTime);
}

public class BodyCreationService : IBodyCreationService
{
    public string CreateBody(string taxId, int operationId, DateTimeOffset currentTime)
    {
        var payload = new
        {
            idOperazioneClient = operationId.ToString(),
            criteriRicerca = new
            {
                codiceFiscale = taxId
            },
            datiRichiesta = new
            {
                dataRiferimentoRichiesta = currentTime.AddDays(-1).Date.ToString("yyyy-MM-dd"),
                motivoRichiesta = "1",
                casoUso = "C019"
            }
        };
        return JsonSerializer.Serialize(payload);
    }
}
using AliveChecker.Application.Database.Entities;
using AliveChecker.Application.Endpoints.Models;
using AliveChecker.Application.Files.Models;

namespace AliveChecker.Application.Utils;

public static class Mapper
{
    public static Person Map(this SuccessResponse response, string taxId)
    {
        return new Person
        {
            TaxId = taxId,
            IsAlive = response.IsAlive,
            FullResponse = response.FullResponse,
            ANPROperationId = response.AnprOperationId,
            CheckDate = response.CheckDate,
            ReferenceDate = response.ReferenceDate,
            StatusDescription = response.StatusDescription,
            DeathDate = response.DeathDate
        };
    }

    public static Person Map(this NotFoundResponse response, string taxId)
    {
        return new Person
        {
            TaxId = taxId,
            IsAlive = null,
            FullResponse = response.FullResponse,
            ANPROperationId = response.AnprOperationId,
            CheckDate = response.CheckDate,
            ReferenceDate = response.ReferenceDate,
            StatusDescription = response.StatusDescription
        };
    }
    public static PeopleWriteData Map(this Person person)
    {
        return new PeopleWriteData
        {
            TaxId = person.TaxId,
            InVita = person.IsAlive,
            IdOperazioneAnpr = person.ANPROperationId ?? string.Empty,
            CheckDate = person.CheckDate?.AddDays(-1).ToString("yyyy-MM-dd") ?? "Unchecked",
            StatusDescription = person.StatusDescription ?? string.Empty,
            DeathDate = person.DeathDate ?? string.Empty
        };
    }
}
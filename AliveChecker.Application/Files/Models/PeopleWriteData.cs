using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace AliveChecker.Application.Files.Models;

public record PeopleWriteData
{
    [Name("CodiceFiscale")]
    public string TaxId { get; set; }
    [Name("InVita")]
    [BooleanTrueValues("S")]
    [BooleanFalseValues("N")]
    public bool? InVita { get; set; }
    [Name("DataDecesso")]
    public string DeathDate { get; set; }
    [Name("IdOperazioneANPR")]
    public string IdOperazioneAnpr { get; set; }
    [Name("DataControllo")]
    public string CheckDate { get; set; }
    [Name("DescrizioneStato")]
    public string StatusDescription { get; set; }
}

public class PeopleWriteDataMap : ClassMap<PeopleWriteData>
{
    public PeopleWriteDataMap()
    {
        Map(m => m.TaxId).Index(0);
        Map(m => m.CheckDate).Index(2);
        Map(m => m.InVita).Index(1);
        Map(m => m.DeathDate).Index(3);
        Map(m => m.IdOperazioneAnpr).Index(4);
        Map(m => m.StatusDescription).Index(5);
    }
}
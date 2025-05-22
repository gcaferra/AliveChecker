using CsvHelper.Configuration;

namespace AliveChecker.Application.Files.Models;

public class PeopleReadData
{
    public string TaxId { get; set; }
}

public class PeopleReadDataMap : ClassMap<PeopleReadData>
{
    public PeopleReadDataMap()
    {
        Map(m => m.TaxId).Index(0);
    }
}
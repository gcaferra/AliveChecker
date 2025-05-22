using System.Globalization;
using AliveChecker.Application.Files.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace AliveChecker.Application.Files;

public interface ICsvReadService
{
    Task<IEnumerable<PeopleReadData>> ImportFile(IFileService service);
}

public class CsvReadService : ICsvReadService
{
    readonly CsvConfiguration _csvConfiguration = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = false
    };

    public async Task<IEnumerable<PeopleReadData>> ImportFile(IFileService service)
    {
        var reader = await service.GetStreamReaderAsync();
        var csvReader = new CsvReader(reader, _csvConfiguration);
        csvReader.Context.RegisterClassMap<PeopleReadDataMap>();

        return csvReader.GetRecords<PeopleReadData>();

    }
}
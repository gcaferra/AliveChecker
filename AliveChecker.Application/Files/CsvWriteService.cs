using System.Globalization;
using AliveChecker.Application.Files.Models;
using CsvHelper;
using CsvHelper.Configuration;

namespace AliveChecker.Application.Files;

public class CsvWriteService : ICsvWriteService
{
    CsvWriter _csv;
    StreamWriter _stringWriter;

    public async Task ExportAsync(PeopleWriteData people)
    {
        _csv.WriteRecord(people);
        await _csv.NextRecordAsync();
        await _csv.FlushAsync();
    }

    public async Task OpenTargetAsync(IFileService fileService)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        _stringWriter = await fileService.GetStreamWriterAsync();
        _csv = new CsvWriter(_stringWriter, config);
        _csv.WriteHeader<PeopleWriteData>();
        await _csv.NextRecordAsync();
        await _csv.FlushAsync();
    }
}
using AliveChecker.Application.Files;
using AliveChecker.Application.Files.Models;
using FluentAssertions;

namespace AliveChecker.Application.Tests;

public class CsvServiceScenarioTests: IDisposable
{
    readonly CsvReadService _readService;
    readonly FileService _fileService;

    public CsvServiceScenarioTests()
    {

        _readService = new CsvReadService();
        _fileService = new FileService("testcsv.txt");
    }


    [Fact]
    public async Task if_the_database_is_empty_the_import_is_performed()
    {
        //Arrange

        //Act
        var peopleData = (await _readService.ImportFile(_fileService)).ToList();

        //Assert
        peopleData.Should().HaveCount(3);

    }

    [Fact]
    public async Task if_the_database_is_true_the_import_is_performed()
    {
        //Arrange

        //Act
        var actual =(await _readService.ImportFile(_fileService)).ToList();
        
        //Assert
        actual.Should().HaveCount(3);

    }

    [Fact]
    public async Task ImportFile_returns_the_number_of_found_records()
    {
        //Arrange

        //Act
        var actual  = await _readService.ImportFile(_fileService);
        
        //Assert
        actual.Should().HaveCount(3);

    }

    [Fact]
    public async Task ImportFile_receive_an_instance_of_FileContentReader()
    {
        //Arrange

        //Act
        var actual  = await _readService.ImportFile(_fileService);

        //Assert
        actual.Should().HaveCount(3);

    }

    [Fact]
    public async Task ExportFile_write_the_headers()
    {
        var writeService = new CsvWriteService();

        await writeService.OpenTargetAsync(_fileService);

        var fileContent = await File.ReadAllTextAsync(_fileService.GetOutputFileName());

        fileContent.Should().Contain("CodiceFiscale,InVita,DataDecesso,IdOperazioneANPR,DataControllo,DescrizioneStato");
    }

    [Fact]
    public async Task Output_header_are_written_once()
    {
        var writeService = new CsvWriteService();

        await writeService.OpenTargetAsync(_fileService);

        var csvOutput = new PeopleWriteData();
        await writeService.ExportAsync(csvOutput);

        var fileContent = await File.ReadAllTextAsync(_fileService.GetOutputFileName());

        fileContent.Should().Contain("CodiceFiscale,InVita,DataDecesso,IdOperazioneANPR,DataControllo,DescrizioneStato");
    }

    [Fact]
    public async Task A_People_Row_is_writen_to_the_file_preserving_the_headers()
    {
        var writeService = new CsvWriteService();

        var checkDate = DateTime.UtcNow.ToShortDateString();
        var people1 = new PeopleWriteData
        {
            TaxId = "taxId1",
            InVita = true,
            DeathDate = null,
            IdOperazioneAnpr = "123456",
            CheckDate = checkDate,
            StatusDescription = "Test1"
        };
        var people2 = new PeopleWriteData
        {
            TaxId = "taxId2",
            InVita = true,
            DeathDate = null,
            IdOperazioneAnpr = "678910",
            CheckDate = checkDate,
            StatusDescription = "Test2"
        };

        // should write the headers
        await writeService.OpenTargetAsync(_fileService);

        await writeService.ExportAsync(people1);

        await writeService.ExportAsync(people2);

        var fileContent = await File.ReadAllTextAsync(_fileService.GetOutputFileName());

        fileContent
            .Should()
            .Contain($"CodiceFiscale,InVita,DataDecesso,IdOperazioneANPR,DataControllo,DescrizioneStato\r\ntaxId1,S,,123456,{checkDate},Test1\r\ntaxId2,S,,678910,{checkDate},Test2\r\n");
    }

    [Fact]
    public async Task A_People_Row_is_writen_to_the_file()
    {
        var writeService = new CsvWriteService();

        await writeService.OpenTargetAsync(_fileService);
        var checkDate = DateTime.UtcNow.ToShortDateString();
        var csvOutput = new PeopleWriteData
            {
                TaxId = "taxID",
                InVita = true,
                DeathDate = null,
                IdOperazioneAnpr = "123456",
                CheckDate = checkDate,
                StatusDescription = "Test"
            };
        await writeService.ExportAsync(csvOutput);

        var fileContent = await File.ReadAllTextAsync(_fileService.GetOutputFileName());

        fileContent.Should().Be($"CodiceFiscale,InVita,DataDecesso,IdOperazioneANPR,DataControllo,DescrizioneStato\r\ntaxID,S,,123456,{checkDate},Test\r\n");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
using AliveChecker.Application.Files.Models;

namespace AliveChecker.Application.Files;

public interface ICsvWriteService
{
    Task ExportAsync(PeopleWriteData people);
    Task OpenTargetAsync(IFileService fileService);
}
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Utils;
using Azure.Storage.Blobs;

namespace AliveChecker.Application.Files;

public interface IFileService
{
    Task<StreamReader> GetStreamReaderAsync();
    Task<StreamWriter> GetStreamWriterAsync();
    string InputFilePath { get; }
}

public class FileService : IFileService
{
    readonly string _filePath;
    string _outputFileName = string.Empty;

    public FileService(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file {filePath} does not exist.");
        }

        _filePath = filePath;
    }

    public Task<StreamReader> GetStreamReaderAsync()
    {
        return Task.FromResult( new StreamReader(_filePath));
    }

    public Task<StreamWriter> GetStreamWriterAsync()
    {
        var stream = File.Open(GetOutputFileName(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        return Task.FromResult(new StreamWriter(stream));
    }

    public string InputFilePath => _filePath;

    internal string GetOutputFileName()
    {
        if (!string.IsNullOrEmpty(_outputFileName)) return _outputFileName;

        var extension = _filePath.FileExtension();

        var outputFilePath = _filePath.Replace(extension, "_out.csv");
        int count = 0;
        while (File.Exists(outputFilePath))
        {
            count++;
            outputFilePath = _filePath.Replace(extension, $"_out_{count}.csv");
        }

        _outputFileName = outputFilePath;
        return outputFilePath;

    }
}

public class BlobService : IFileService
{
    readonly BlobClient _blobClient;
    readonly IDateProvider _dateProvider;
    readonly ClientConfiguration _clientConfiguration;
    string _outputFileName = string.Empty;

    public BlobService(BlobClient blobClient, IDateProvider dateProvider, ClientConfiguration clientConfiguration)
    {
        _blobClient = blobClient;
        _dateProvider = dateProvider;
        _clientConfiguration = clientConfiguration;
    }

    public async Task<StreamReader> GetStreamReaderAsync()
    {
        var memoryStream = new MemoryStream();
        await _blobClient.DownloadToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return new StreamReader(memoryStream);
    }

    public async Task<StreamWriter> GetStreamWriterAsync()
    {
        var container = new BlobContainerClient(_clientConfiguration.BlobConnectionString, _clientConfiguration.OutputContainer);
        await container.CreateIfNotExistsAsync();

        var client = container.GetBlobClient(GetOutputFileName());
        var stream = await client.OpenWriteAsync(true);
        return new StreamWriter(stream);
    }

    public string InputFilePath => _blobClient.Name;

    string GetOutputFileName()
    {
        if (!string.IsNullOrEmpty(_outputFileName)) return _outputFileName;


        _outputFileName = InputFilePath.Replace(InputFilePath.FileExtension(), $"_out_{_dateProvider.UtcNow:yyy-MM-yy-hhmmss}.csv");
        return _outputFileName;
    }
}

public static class FileServiceExtensions
{
    public static string FileExtension(this string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Extension;
    }
}
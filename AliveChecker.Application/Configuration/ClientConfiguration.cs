namespace AliveChecker.Application.Configuration;

public record ClientConfiguration
{
    public string KeyId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SignatureAudience { get; init; } = "";
    public string PurposeId { get; init; } = "";
    public string PrivateKey { get; init; } = "";
    public string CsvFilePath { get; init; } = "";
    public string ServiceUrl { get; init; } = "";
    public string UserId { get; init; } = "";
    public string AuthenticationUrl { get; init; } = "";
    public bool InitializeDb { get; init; }
    public string BlobConnectionString { get; init; } = "";
    public string InputContainer { get; init; } = "";
    public string OutputContainer { get; init; } = "";
}
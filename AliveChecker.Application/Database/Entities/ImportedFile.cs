namespace AliveChecker.Application.Database.Entities;

public class ImportedFile
{
    public int Id { get; set; }
    public string Path { get; set; }
    public DateTime ImportedTime { get; set; }
}
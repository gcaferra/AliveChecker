namespace AliveChecker.Application.Database.Entities;

public class Person
{
    public int Id { get; set; }
    public string TaxId { get; set; }
    public DateTime? CheckDate { get; set; }
    public DateTime? ReferenceDate { get; set; }
    public bool? IsAlive { get; set; }
    public string? FullResponse { get; set; }
    public string? ANPROperationId { get; set; }
    public string? StatusDescription { get; set; }
    public string? DeathDate { get; set; }
}
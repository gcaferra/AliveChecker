namespace AliveChecker.Application.Utils;

public interface IDateProvider
{
    DateTimeOffset UtcNow { get; }
}

public sealed class DateProvider : IDateProvider
{
    public DateTimeOffset UtcNow => DateTime.UtcNow;
}
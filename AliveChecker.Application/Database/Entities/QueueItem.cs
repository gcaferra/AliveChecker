using System.ComponentModel.DataAnnotations;

namespace AliveChecker.Application.Database.Entities;

public class QueueItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    [MaxLength(50)] public string TaxId { get; init; } = string.Empty;
    public DateTime EnqueueTime { get; set; }
    public QueueItemStatus Status { get; set; } = QueueItemStatus.Created;

    public int RetryCount { get; set; } = 0;
}

public enum QueueItemStatus {Created, Queued, Picked }
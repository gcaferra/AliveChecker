using AliveChecker.Application.Database.Entities;
using AliveChecker.Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace AliveChecker.Application.Database;

public interface ICheckerRepository
{
    void SavePerson(Person person);
    QueueItem? Enqueue(string taxId);
    IEnumerable<QueueItem> DeQueue();
    void Ack(Guid itemId);
    void NAck(Guid itemId);
    int QueueCount();
    bool FileIsImported(string pathToCsvfileCsv);
    void FileCompleted(string pathToCsvfileCsv);
    void InitializeDatabase(bool initializeDb);
}

public class CheckerRepository(CheckerContext context, IDateProvider dateProvider) : ICheckerRepository
{
    readonly Func<QueueItem?, bool> _commonQuery = q => q is { Status: QueueItemStatus.Queued, RetryCount: < 10 };
    public IEnumerable<QueueItem> DeQueue()
    {

        while (true)
        {
            var item = context
                .Queue
                .OrderBy(x => x.EnqueueTime)
                .FirstOrDefault(_commonQuery);

            if(item ==null) yield break;

            item.Status = QueueItemStatus.Picked;
            context.SaveChanges();
            yield return item;

        }
    }

    public void SavePerson(Person person)
    {
        context.Entry(person).State = EntityState.Added;
        context.Peoples.Add(person);
        context.SaveChanges();
    }

    public QueueItem? Enqueue(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return null;

        var queueItem = new QueueItem { TaxId = taxId, EnqueueTime = dateProvider.UtcNow.DateTime, Status = QueueItemStatus.Queued};
        context.Queue.Add(queueItem);
        context.Entry(queueItem).State = EntityState.Added;
        context.SaveChanges();
        return queueItem;
    }

    public void Ack(Guid itemId)
    {
        context.Queue.Where(q => q!.Id == itemId).ExecuteDelete();
    }

    public void NAck(Guid itemId)
    {
        var queueItem = context.Queue.First(x => x.Id ==itemId);
        queueItem.Status = QueueItemStatus.Queued;
        queueItem.EnqueueTime = dateProvider.UtcNow.DateTime;
        queueItem.RetryCount += 1;
        context.SaveChanges();
    }

    public int QueueCount() => context.Queue.Count(_commonQuery);
    public bool FileIsImported(string pathToCsvfileCsv) => context.Files.Any(f => f.Path == pathToCsvfileCsv);
    public void FileCompleted(string pathToCsvfileCsv)
    {
        context.Add(new ImportedFile(){ Path = pathToCsvfileCsv, ImportedTime = dateProvider.UtcNow.DateTime});
        context.SaveChanges();
    }

    public void InitializeDatabase(bool initializeDb)
    {
        if (initializeDb) context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }
}
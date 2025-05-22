using AliveChecker.Application.Database;
using AliveChecker.Application.Database.Entities;
using AliveChecker.Application.Utils;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AliveChecker.Application.Tests;

public class DatabaseContextScenario : IClassFixture<DatabaseFixture>
{
    readonly IDateProvider _dataProvider;
    readonly CheckerRepository _sut;

    public DatabaseContextScenario(DatabaseFixture fixture)
    {
        Fixture = fixture;
        _dataProvider = Substitute.For<IDateProvider>();
        _dataProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        _sut = new CheckerRepository(fixture.Context,_dataProvider);

    }

    DatabaseFixture Fixture { get; }

    [Fact]
    public void Enqueue_Add_a_record_to_the_Queue_table()
    {
        // Arrange
        var prevCount = Fixture.Context.Queue.Count();

        // Act
        _sut.Enqueue("TaxId");


        // Assert
        var context = Fixture.Context;
        Assert.Equal(prevCount + 1, context.Queue.Count());
    }

    [Fact]
    public void Enqueue_Will_not_add_Empty_Strings_to_the_Queue()
    {
        // Arrange
        var prevCount = Fixture.Context.Queue.Count();

        // Act
        _sut.Enqueue(string.Empty);

        // Assert
        var context = Fixture.Context;
        Assert.Equal(prevCount, context.Queue.Count());
    }

    [Fact]
    public void Dequeue_set_the_record_to_Pending_status()
    {
        // Arrange
        _sut.Enqueue($"TaxId{Guid.NewGuid()}");

        // Act
        foreach (var queueItem in _sut.DeQueue())
        {
            //Iterating all pending items
        }

        // Assert
        using var context = new CheckerContext(Fixture.Options);
        context.Queue.Should().AllSatisfy(x =>x.Status = QueueItemStatus.Picked);
    }

    [Fact]
    public void Dequeued_items_can_be_Acknowledged()
    {
        // Arrange
        _sut.Enqueue("TaxId1");
        var item = _sut.DeQueue().First();

        // Act
        _sut.Ack(item.Id);

        // Assert
        var context = new CheckerContext(Fixture.Options);
        context.Queue.Should().NotContain(x => x.Id == item.Id);
    }

    [Fact]
    public void Dequeued_items_can_be_NotAcknowledged()
    {
        // Arrange
        DateTimeOffset reEnqueueTime = DateTimeOffset.UtcNow.AddHours(1);
        _dataProvider.UtcNow.Returns(reEnqueueTime);
        _sut.Enqueue("TaxId1");
        var item = _sut.DeQueue().First();

        // Act
        _sut.NAck(item.Id);

        // Assert
        var context = new CheckerContext(Fixture.Options);
        var actual = context.Queue.Single(x =>x.Id == item.Id);

        actual.Status.Should().Be(QueueItemStatus.Queued);
        actual.EnqueueTime.Should().Be(reEnqueueTime.DateTime);
    }

    [Fact]
    public void NAcked_Items_has_RetryCount_increased_by_one()
    {
        // Arrange
        DateTimeOffset reEnqueueTime = DateTimeOffset.UtcNow.AddHours(1);
        _dataProvider.UtcNow.Returns(reEnqueueTime);
        _sut.Enqueue("TaxId1");
        var item = _sut.DeQueue().First();

        // Act
        _sut.NAck(item.Id);

        // Assert
        var context = new CheckerContext(Fixture.Options);
        var actual = context.Queue.Single(x =>x.Id == item.Id);

        actual.RetryCount.Should().Be(1);
    }

    [Fact]
    public void NotAcknowledged_items_are_dequeued_later_at_the_end_the_queue()
    {
        // Arrange
        Fixture.Context.Queue.ExecuteDelete(); //this test is focused on order and can't be interfered by others
        _dataProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        var item1 = _sut.Enqueue("TaxId1");
        var item2 = _sut.Enqueue("TaxId2");
        var item3 = _sut.Enqueue("TaxId3");

        DateTimeOffset reEnqueueTime = DateTimeOffset.UtcNow.AddHours(1);
        _dataProvider.UtcNow.Returns(reEnqueueTime);

        // Act
        var item = _sut.DeQueue().First();
        _sut.NAck(item.Id);

        // Assert
        var context = new CheckerContext(Fixture.Options);
        context.Queue.OrderBy(x => x.EnqueueTime)
            .Select(x => x.TaxId)
            .Should()
            .ContainInOrder([item2.TaxId, item3.TaxId, item1.TaxId]);

    }

    [Fact]
    public void Once_item_is_Nack_the_next_one_can_be_dequeued()
    {
        // Arrange
        DateTimeOffset reEnqueueTime = DateTimeOffset.UtcNow.AddHours(1);
        var item1 = _sut.Enqueue("TaxId1");
        var item2 = _sut.Enqueue("TaxId2");
        var item3 = _sut.Enqueue("TaxId3");
        _dataProvider.UtcNow.Returns(reEnqueueTime);

        var queueItem1 = _sut.DeQueue().First();
        // Act
        _sut.NAck(queueItem1.Id);
        var actual = _sut.DeQueue().First();

        // Assert
        actual.Id.Should().Be(item2.Id);

    }

    [Fact]
    public void Dequeue_items_get_next_item_even_not_yet_Ack_or_Nack()
    {
        // Arrange
        DateTimeOffset reEnqueueTime = DateTimeOffset.UtcNow.AddHours(2);
        var item1 = _sut.Enqueue("TaxId1");
        var item2 = _sut.Enqueue("TaxId2");
        var item3 = _sut.Enqueue("TaxId3");
        _dataProvider.UtcNow.Returns(reEnqueueTime);


        // Act
        // Assert
        _sut.DeQueue().Should().ContainInOrder(new[] { item1, item2, item3 });


    }

    [Fact]
    public void Person_data_are_saved_into_the_table()
    {
        //Arrange
        var person = new Person
        {
            TaxId = $"TaxId{Guid.NewGuid()}",
            CheckDate = DateTime.UtcNow,
            ReferenceDate = DateTime.UtcNow.AddDays(-1),
            IsAlive = true,
            FullResponse = "thisIsTheFullResponse",
            ANPROperationId = "ANPROperationId",
            StatusDescription = "TestStatus",
        };

        //Act
        _sut.SavePerson(person);

        //Assert
        var context = new CheckerContext(Fixture.Options);
        context.Peoples.Should().ContainEquivalentOf(person);
    }

    [Fact]
    public void if_a_file_is_imported_the_method_should_return_true()
    {
        //Arrange
        var path = "pathToCsvfileCsv";
        var context = new CheckerContext(Fixture.Options);
        context.Files.Add(new ImportedFile {Path = path});
        context.SaveChanges();

        //Act
        var actual = _sut.FileIsImported(path);

        //Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public void if_a_file_is_not_imported_the_method_should_return_false()
    {
        //Arrange
        var path = "pathToCsvfileCsv";

        //Act
        var actual = _sut.FileIsImported(path);

        //Assert
        actual.Should().BeFalse();
    }

    [Fact]
    public void once_fully_processed_a_file_is_marked_as_complete()
    {
        //Arrange
        var path = $"pathToCsvfileCsv{Guid.NewGuid()}";

        //Act
        _sut.FileCompleted(path);

        //Assert
        var context = new CheckerContext(Fixture.Options);
        context.Files.Should().Contain(x => x.Path == path);

    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(7, true)]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [InlineData(11, false)]
    public void Items_with_more_than_10_retry_cant_be_dequeued_anymore(int retryCount, bool expected)
    {
        //Arrange
        var context = new CheckerContext(Fixture.Options);
        context.Queue.ExecuteDelete(); //this test is focused on retry count and can't be interfered by others
        context.Queue.Add(new QueueItem
        {
            Id = default,
            TaxId = "taxId",
            EnqueueTime = DateTime.Now,
            Status = QueueItemStatus.Queued,
            RetryCount = retryCount
        });
        context.SaveChanges();

        //Act
        var actual =_sut.DeQueue();

        //Assert
        if (expected)
        {
            actual.Should().NotBeEmpty();
        }
        else
        {
            actual.Should().BeEmpty();
        }
    }

    [Fact]
    public void Dequeuing_items_with_more_than_10_retry_results_in_EmptyCollection()
    {
        //Arrange
        var context = new CheckerContext(Fixture.Options);
        context.Queue.ExecuteDelete(); //this test is focused on retry count and can't be interfered by others
        context.Queue.Add(new QueueItem
        {
            Id = default,
            TaxId = "taxId",
            EnqueueTime = DateTime.Now,
            Status = QueueItemStatus.Queued,
            RetryCount = 10
        });
        context.SaveChanges();

        //Act
        //Assert
        var queueItems = _sut.DeQueue();

        queueItems.Should().BeEmpty();


    }

    [Fact]
    public void Dequeuing_items_keep_going_fetching_items_until_they_are_fetchable()
    {
        //Arrange
        var context = new CheckerContext(Fixture.Options);
        context.Queue.ExecuteDelete();
        context.AddQueuedItems(4);
        //Act
        var callCount = 0;
        foreach (var item in _sut.DeQueue())
        {
            _sut.NAck(item.Id);
            callCount++;
        }

        //Assert
        callCount.Should().Be(40);

    }

    [Fact]
    public void Dequeuing_items_are_consumed_one_by_one()
    {
        //Arrange
        var context = new CheckerContext(Fixture.Options);
        context.Queue.ExecuteDelete();
        context.AddQueuedItems(4);
        //Act
        using var iterator = _sut.DeQueue().GetEnumerator();

        iterator.MoveNext();
        context.Queue.Where(x => x.Status == QueueItemStatus.Queued).Should().HaveCount(3);
        iterator.MoveNext();
        context.Queue.Where(x => x.Status == QueueItemStatus.Queued).Should().HaveCount(2);
        iterator.MoveNext();
        context.Queue.Where(x => x.Status == QueueItemStatus.Queued).Should().HaveCount(1);
        iterator.MoveNext();
        context.Queue.Where(x => x.Status == QueueItemStatus.Queued).Should().HaveCount(0);
    }

}

public static class DatabaseContextScenarioExtensions
{
    public static void AddQueuedItems(this CheckerContext context, int count = 1)
    {
        foreach (var index in Enumerable.Range(0, count))
        {
            context.Queue.Add(new QueueItem
            {
                Id = default,
                TaxId = $"taxId{index}",
                EnqueueTime = DateTime.Now,
                Status = QueueItemStatus.Queued
            });
        }

        context.SaveChanges();
    }

    public static void AddQueuedItemsTemplate(this CheckerContext context, QueueItem? queueItem, int count = 1)
    {
        foreach (var unused in Enumerable.Range(0, count))
        {
            context.Queue.Add(queueItem);
        }

        context.SaveChanges();
    }
}
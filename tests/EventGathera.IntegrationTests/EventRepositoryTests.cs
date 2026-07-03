using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using Testcontainers.PostgreSql;

namespace EventGathera.IntegrationTests;

public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("eventapi")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        await InitializeAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        var context = new AppDbContext(options);
        return context;
    }

    private async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    [Fact]
    public async Task AddEventAsync_SavesEventToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);

        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = eventId
        };

        // Act
        await repository.AddEventAsync(eventEntity, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal(eventId, saved.Id);
        Assert.Equal("Test Event", saved.Title);
        Assert.Equal(10, saved.TotalSeats);
        Assert.Equal(10, saved.AvailableSeats);
        Assert.Equal("Test Description", saved.Description);
    }

    [Fact]
    public async Task GetEventByIdAsync_ReturnsEventFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = eventId
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var result = await repository.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(eventId, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal(10, result.TotalSeats);
        Assert.Equal(10, result.AvailableSeats);
    }

    [Fact]
    public async Task GetEventByIdAsync_WithInvalidId_ReturnsNull()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var repository = new EventRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetEventByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateEventAsync_SavesChangesToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Original Title",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Original Description"
        )
        {
            Id = eventId
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var eventToUpdate = await repository.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(eventToUpdate);

        eventToUpdate.Title = "Updated Title";
        eventToUpdate.Description = "Updated Description";
        eventToUpdate.TotalSeats = 20;

        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(saved);
        Assert.Equal("Updated Title", saved.Title);
        Assert.Equal("Updated Description", saved.Description);
        Assert.Equal(20, saved.TotalSeats);
    }

    [Fact]
    public async Task RemoveEventAsync_DeletesEventFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = eventId
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var eventToDelete = await repository.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(eventToDelete);

        repository.RemoveEvent(eventToDelete, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var deleted = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task RemoveEventAsync_WithBookings_CascadeDeletesBookings()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = eventId
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Создаем бронирования
        var booking1 = new Booking(eventId);
        var booking2 = new Booking(eventId);
        context.Bookings.AddRange(booking1, booking2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var eventToDelete = await repository.GetEventByIdAsync(eventId, TestContext.Current.CancellationToken);
        Assert.NotNull(eventToDelete);

        repository.RemoveEvent(eventToDelete, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var deletedEvent = await verifyContext.Events
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(deletedEvent);

        var deletedBookings = await verifyContext.Bookings
            .Where(b => b.EventId == eventId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(deletedBookings);
    }

    [Fact]
    public async Task GetAllEventsQuery_ReturnsAllEventsFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var eventId3 = Guid.NewGuid();

        var event1 = new Event(
            title: "Event 1",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Event 1"
        )
        {
            Id = eventId1
        };

        var event2 = new Event(
            title: "Event 2",
            startAt: DateTime.UtcNow.AddDays(3),
            endAt: DateTime.UtcNow.AddDays(4),
            totalSeats: 20,
            description: "Test Event 2"
        )
        {
            Id = eventId2
        };

        var event3 = new Event(
            title: "Event 3",
            startAt: DateTime.UtcNow.AddDays(5),
            endAt: DateTime.UtcNow.AddDays(6),
            totalSeats: 30,
            description: "Test Event 3"
        )
        {
            Id = eventId3
        };

        context.Events.AddRange(event1, event2, event3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var query = repository.GetAllEventsQuery();
        var events = await query.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, events.Count);
        Assert.Contains(events, e => e.Id == eventId1);
        Assert.Contains(events, e => e.Id == eventId2);
        Assert.Contains(events, e => e.Id == eventId3);
    }

    [Fact]
    public async Task GetAllEventsQuery_WithFilter_ReturnsFilteredEvents()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var event1 = new Event(
            title: "Tech Conference",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 100,
            description: "Tech event"
        )
        {
            Id = eventId1
        };

        var event2 = new Event(
            title: "Music Festival",
            startAt: DateTime.UtcNow.AddDays(3),
            endAt: DateTime.UtcNow.AddDays(4),
            totalSeats: 200,
            description: "Music event"
        )
        {
            Id = eventId2
        };

        context.Events.AddRange(event1, event2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new EventRepository(context);

        // Act
        var query = repository.GetAllEventsQuery()
            .Where(e => e.Title.Contains("Tech"));
        var events = await query.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(events);
        Assert.Equal("Tech Conference", events[0].Title);
    }

    [Fact]
    public async Task Migrate_CreatesEventsBookingsAndForeignKey()
    {
        await ResetDatabaseAsync();

        await using var context = CreateContext();

        var tables = await context.Database.SqlQueryRaw<string>(@"
        select table_name
        from information_schema.tables
        where table_schema = 'public'
          and table_name in ('events', 'bookings')
        order by table_name")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains("events", tables);
        Assert.Contains("bookings", tables);

        var foreignKeys = await context.Database.SqlQueryRaw<string>(@"
        select tc.constraint_name
        from information_schema.table_constraints tc
        join information_schema.key_column_usage kcu
          on tc.constraint_name = kcu.constraint_name
         and tc.table_schema = kcu.table_schema
        where tc.constraint_type = 'FOREIGN KEY'
          and tc.table_schema = 'public'
          and tc.table_name = 'bookings'
          and kcu.column_name = 'event_id'")
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.NotEmpty(foreignKeys);
    }

}
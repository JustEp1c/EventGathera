using EventGathera.Domain;
using EventGathera.Domain.Enums;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EventGathera.IntegrationTests;

public class BookRepositoryTests : IAsyncLifetime
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

    private async Task<Guid> CreateTestUserAsync(AppDbContext context)
    {
        var user = new User(
            login: $"user_{Guid.NewGuid():N}".Substring(0, 30),
            passwordHash: "hashedpassword",
            role: Roles.User
        );
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task AddBookingAsync_SavesBookingToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = await CreateTestUserAsync(context);

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

        var repository = new BookingRepository(context);
        var booking = new Booking(
            eventId,
            userId
        );

        // Act
        await repository.AddBookingAsync(booking, TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(booking.Id, saved.Id);
        Assert.Equal(eventId, saved.EventId);
        Assert.Equal(BookingStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBookingFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = await CreateTestUserAsync(context);

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

        var booking = new Booking(
            eventId,
            userId
        );
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);

        // Act
        var result = await repository.GetBookingByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(booking.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task UpdateBookingAsync_SavesChangesToDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = await CreateTestUserAsync(context);

        var eventId = Guid.NewGuid();
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = eventId,
        };
        context.Events.Add(eventEntity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking = new Booking(
            eventId,
            userId
        );
        context.Bookings.Add(booking);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);

        // Act
        booking.Confirm();
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Confirmed, saved.Status);
        Assert.NotNull(saved.ProcessedAt);
    }

    [Fact]
    public async Task GetAllBookingsQuery_ReturnsAllBookingsFromDatabase()
    {
        await ResetDatabaseAsync();

        // Arrange
        await using var context = CreateContext();
        var userId = await CreateTestUserAsync(context);

        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var event1 = new Event(
            title: "Event 1",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Event 1"
        )
        {
            Id = eventId1,
        };

        var event2 = new Event(
            title: "Event 2",
            startAt: DateTime.UtcNow.AddDays(3),
            endAt: DateTime.UtcNow.AddDays(4),
            totalSeats: 20,
            description: "Test Event 2"
        )
        {
            Id = eventId2,
        };

        context.Events.AddRange(event1, event2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var booking1 = new Booking(eventId1, userId);
        var booking2 = new Booking(eventId2, userId);
        var booking3 = new Booking(eventId1, userId);

        context.Bookings.AddRange(booking1, booking2, booking3);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new BookingRepository(context);

        // Act
        var query = repository.GetAllBookingsQuery();
        var bookings = await query.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, bookings.Count);
        Assert.Contains(bookings, b => b.Id == booking1.Id);
        Assert.Contains(bookings, b => b.Id == booking2.Id);
        Assert.Contains(bookings, b => b.Id == booking3.Id);
    }
}

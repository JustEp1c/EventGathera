using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Implementations;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
using EventGathera.Domain.Enums;
using EventGathera.Domain.Exceptions;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Tests;

public class BookingServiceCreateBookingTests
{
    private readonly AppDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _existingEventId;
    private readonly int _initialTotalSeats = 100;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BookingServiceCreateBookingTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _bookingService = _serviceProvider.GetRequiredService<IBookingService>();

        _existingEventId = Guid.NewGuid();
        var testEvent = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: _initialTotalSeats,
            description: "Test Description"
        )
        {
            Id = _existingEventId
        };

        _dbContext.Events.Add(testEvent);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task CreateEvent_WithValidEventId_ShouldReturnCreatedBookingWithPendingStatus()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(_existingEventId, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateEvent_WithValidEventId_ShouldReturnTwoCreatedBookingsWithUniqueIds()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result1 = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        var result2 = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(_existingEventId, result1.EventId);
        Assert.Equal(_existingEventId, result2.EventId);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task CreateEvent_WithNonExistingEventId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        Guid nonExistingEventId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _bookingService.CreateBookingAsync(nonExistingEventId, _testUserId, ct));

        Assert.Equal($"Событие с ID {nonExistingEventId} не найдено", exception.Message);
    }

    [Fact]
    public async Task CreateEvent_WithNonDeletedEventId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventToDelete = await _dbContext.Events
            .FirstOrDefaultAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);

        if (eventToDelete != null)
        {
            _dbContext.Events.Remove(eventToDelete);
            await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct));

        Assert.Equal($"Событие с ID {_existingEventId} не найдено", exception.Message);
    }

    [Fact]
    public async Task CreateBooking_ShouldDecreaseAvailableSeatsByOne()
    {
        // Arrange
        var ct = CancellationToken.None;

        var eventBefore = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventBefore.AvailableSeats;

        // Act
        var booking = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);

        // Assert
        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(initialAvailableSeats - 1, eventAfter.AvailableSeats);
        Assert.Equal(booking.EventId, _existingEventId);
    }

    [Fact]
    public async Task CreateMultipleBookings_UpToLimit_ShouldAllSucceedAndHaveUniqueIds()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int totalSeats = eventEntity.TotalSeats;

        var bookingIds = new List<Guid>();

        // Act
        for (int i = 0; i < totalSeats; i++)
        {
            var userId = Guid.NewGuid();
            var booking = await _bookingService.CreateBookingAsync(_existingEventId, userId, ct);
            bookingIds.Add(booking.Id);

            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        // Assert
        Assert.Equal(totalSeats, bookingIds.Distinct().Count());

        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int totalSeats = eventEntity.TotalSeats;


        for (int i = 0; i < totalSeats; i++)
        {
            var userId = Guid.NewGuid();
            await _bookingService.CreateBookingAsync(_existingEventId, userId, ct);
        }

        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, eventAfter.AvailableSeats);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct));

        Assert.Equal("Нет свободных мест на это событие", exception.Message);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeatsAvailable_ShouldNotCreateBookingAndKeepSeatsAtZero()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int totalSeats = eventEntity.TotalSeats;

        for (int i = 0; i < totalSeats; i++)
        {
            var userId = Guid.NewGuid();
            await _bookingService.CreateBookingAsync(_existingEventId, userId, ct);
        }

        int bookingsCountBefore = await _dbContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct));

        // Assert
        int bookingsCountAfter = await _dbContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(bookingsCountBefore, bookingsCountAfter);

        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_ForPastEvent_ShouldThrowExpiredEventBookingException()
    {
        // Arrange
        var ct = CancellationToken.None;

        var pastEventId = Guid.NewGuid();
        var pastEvent = new Event(
            title: "Past Event",
            startAt: DateTime.UtcNow.AddDays(-2),
            endAt: DateTime.UtcNow.AddDays(-1),
            totalSeats: 10,
            description: "Event that already started"
        )
        {
            Id = pastEventId
        };
        _dbContext.Events.Add(pastEvent);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExpiredEventBookingException>(() =>
            _bookingService.CreateBookingAsync(pastEventId, _testUserId, ct));

        Assert.Equal($"Событие с ID {pastEventId} уже началось", exception.Message);
        Assert.Equal(pastEventId, exception.EventId);
    }

    [Fact]
    public async Task CreateBooking_WhenActiveBookingLimitReached_ShouldThrowExceedingActiveBookingLimitException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();

        var events = new List<Event>();
        for (int i = 0; i < 10; i++)
        {
            var eventId = Guid.NewGuid();
            var eventEntity = new Event(
                title: $"Event {i}",
                startAt: DateTime.UtcNow.AddDays(10 + i),
                endAt: DateTime.UtcNow.AddDays(11 + i),
                totalSeats: 10,
                description: $"Test Event {i}"
            )
            {
                Id = eventId
            };
            events.Add(eventEntity);
        }
        _dbContext.Events.AddRange(events);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (int i = 0; i < 10; i++)
        {
            await _bookingService.CreateBookingAsync(events[i].Id, userId, ct);
        }

        // Проверяем, что у пользователя 10 активных броней
        var activeBookingsCount = await _dbContext.Bookings
            .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Pending,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, activeBookingsCount);

        // Act & Assert - пытаемся создать 11-ю бронь
        var exception = await Assert.ThrowsAsync<ExceedingActiveBookingLimitException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, userId, ct));

        Assert.Equal($"Не удалось создать бронь, превышен лимит у пользователя с ID {userId}", exception.Message);
        Assert.Equal(userId, exception.UserId);

        // Проверяем, что новая бронь не создалась
        var finalBookingsCount = await _dbContext.Bookings
            .CountAsync(b => b.UserId == userId,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, finalBookingsCount);
    }

    [Fact]
    public async Task CreateBooking_BookingLimitsForDifferentUsers_ShouldNotAffectEachOther()
    {
        // Arrange
        var ct = CancellationToken.None;
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var events = new List<Event>();
        for (int i = 0; i < 20; i++)
        {
            var eventId = Guid.NewGuid();
            var eventEntity = new Event(
                title: $"Event {i}",
                startAt: DateTime.UtcNow.AddDays(10 + i),
                endAt: DateTime.UtcNow.AddDays(11 + i),
                totalSeats: 10,
                description: $"Test Event {i}"
            )
            {
                Id = eventId
            };
            events.Add(eventEntity);
        }
        _dbContext.Events.AddRange(events);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (int i = 0; i < 10; i++)
        {
            await _bookingService.CreateBookingAsync(events[i].Id, user1Id, ct);
        }

        for (int i = 0; i < 5; i++)
        {
            await _bookingService.CreateBookingAsync(events[10 + i].Id, user2Id, ct);
        }

        // Проверяем, что у User1 10 броней
        var user1Bookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user1Id,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, user1Bookings);

        // Проверяем, что у User2 5 броней
        var user2Bookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(5, user2Bookings);

        for (int i = 0; i < 5; i++)
        {
            await _bookingService.CreateBookingAsync(events[15 + i].Id, user2Id, ct);
        }

        var user2BookingsAfter = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, user2BookingsAfter);

        var exception = await Assert.ThrowsAsync<ExceedingActiveBookingLimitException>(() =>
            _bookingService.CreateBookingAsync(events[19].Id, user1Id, ct));

        Assert.Equal($"Не удалось создать бронь, превышен лимит у пользователя с ID {user1Id}", exception.Message);

        // Проверяем, что лимиты независимы - User1 все еще имеет 10 броней
        var user1FinalBookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user1Id,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, user1FinalBookings);

        // User2 все еще имеет 10 броней
        var user2FinalBookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10, user2FinalBookings);

        // Общее количество броней должно быть 20
        var totalBookings = await _dbContext.Bookings
            .CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(20, totalBookings);
    }

    public void Dispose()
    {
        // Очищаем InMemory БД после каждого теста
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}


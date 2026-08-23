using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Application.Services.Implementations;
using EventGathera.Bookings.Application.Services.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Domain.Exceptions;
using EventGathera.Bookings.Infrastructure.DataAccess;
using EventGathera.Bookings.Infrastructure.Repositories.Implementations;
using EventGathera.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EventGathera.Tests;

public class BookingServiceCreateBookingTests : IDisposable
{
    private readonly BookingsDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BookingServiceCreateBookingTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<BookingsDbContext>();
        _bookingService = _serviceProvider.GetRequiredService<IBookingService>();
    }

    [Fact]
    public async Task CreateBooking_WithValidEventId_ShouldReturnCreatedBookingWithPendingStatus()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId, _testUserId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Equal(eventId, result.EventId);
        Assert.Equal(_testUserId, result.UserId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
        Assert.Null(result.ProcessedAt);

        // Проверяем, что бронь сохранена в БД
        var savedBooking = await _dbContext.Bookings
            .FirstOrDefaultAsync(b => b.Id == result.Id, cancellationToken: ct);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);

        // Проверяем, что Outbox сообщение создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCreated")
            .ToListAsync(ct);
        Assert.Single(outboxMessages);
        Assert.Equal("BookingCreated", outboxMessages[0].Type);
        Assert.Equal(OutboxStatus.Pending, outboxMessages[0].Status);
    }

    [Fact]
    public async Task CreateBooking_WithValidEventId_ShouldReturnTwoCreatedBookingsWithUniqueIds()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        // Act
        var result1 = await _bookingService.CreateBookingAsync(eventId, _testUserId, ct);
        var result2 = await _bookingService.CreateBookingAsync(eventId, _testUserId, ct);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(eventId, result1.EventId);
        Assert.Equal(eventId, result2.EventId);
        Assert.NotEqual(result1.Id, result2.Id);

        // Проверяем, что 2 Outbox сообщения созданы
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCreated")
            .ToListAsync(ct);
        Assert.Equal(2, outboxMessages.Count);
    }

    [Fact]
    public async Task CreateBooking_WithNonExistingEventId_ShouldStillCreateBooking()
    {
        // Arrange
        var ct = CancellationToken.None;
        Guid nonExistingEventId = Guid.NewGuid();

        // Act
        var result = await _bookingService.CreateBookingAsync(nonExistingEventId, _testUserId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nonExistingEventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);

        // Проверяем, что Outbox сообщение создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCreated")
            .ToListAsync(ct);
        Assert.Single(outboxMessages);
    }

    [Fact]
    public async Task CreateBooking_ShouldCreateOutboxMessageWithCorrectData()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        // Act
        var result = await _bookingService.CreateBookingAsync(eventId, _testUserId, ct);

        // Assert
        var outboxMessage = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(o => o.Type == "BookingCreated", ct);
        Assert.NotNull(outboxMessage);

        // Проверяем содержимое Outbox сообщения
        var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(outboxMessage.Payload);
        Assert.NotNull(bookingCreated);
        Assert.Equal(result.Id, bookingCreated.BookingId);
        Assert.Equal(result.EventId, bookingCreated.EventId);
        Assert.Equal(result.UserId, bookingCreated.UserId);
        Assert.True(bookingCreated.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateMultipleBookings_UpToLimit_ShouldAllSucceedAndHaveUniqueIds()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bookingIds = new List<Guid>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var booking = await _bookingService.CreateBookingAsync(eventId, userId, ct);
            bookingIds.Add(booking.Id);
            Assert.Equal(BookingStatus.Pending, booking.Status);
        }

        // Assert
        Assert.Equal(10, bookingIds.Distinct().Count());

        // Проверяем, что 10 Outbox сообщений созданы
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCreated")
            .ToListAsync(ct);
        Assert.Equal(10, outboxMessages.Count);
    }

    [Fact]
    public async Task CreateBooking_WhenActiveBookingLimitReached_ShouldThrowExceedingActiveBookingLimitException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Создаем 10 броней (достигаем лимита)
        for (int i = 0; i < 10; i++)
        {
            await _bookingService.CreateBookingAsync(eventId, userId, ct);
        }

        // Проверяем, что у пользователя 10 активных броней
        var activeBookingsCount = await _dbContext.Bookings
            .CountAsync(b => b.UserId == userId && b.Status == BookingStatus.Pending,
                cancellationToken: ct);
        Assert.Equal(10, activeBookingsCount);

        // Act & Assert - пытаемся создать 11-ю бронь
        var exception = await Assert.ThrowsAsync<ExceedingActiveBookingLimitException>(() =>
            _bookingService.CreateBookingAsync(eventId, userId, ct));

        Assert.Equal($"Не удалось создать бронь, превышен лимит у пользователя с ID {userId}", exception.Message);
        Assert.Equal(userId, exception.UserId);

        // Проверяем, что новая бронь не создалась
        var finalBookingsCount = await _dbContext.Bookings
            .CountAsync(b => b.UserId == userId,
                cancellationToken: ct);
        Assert.Equal(10, finalBookingsCount);
    }

    [Fact]
    public async Task CreateBooking_BookingLimitsForDifferentUsers_ShouldNotAffectEachOther()
    {
        // Arrange
        var ct = CancellationToken.None;
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // User1 создает 10 броней (достигает лимита)
        for (int i = 0; i < 10; i++)
        {
            await _bookingService.CreateBookingAsync(eventId, user1Id, ct);
        }

        // User2 создает 5 броней (не достигает лимита)
        for (int i = 0; i < 5; i++)
        {
            await _bookingService.CreateBookingAsync(eventId, user2Id, ct);
        }

        // Проверяем, что у User1 10 броней
        var user1Bookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user1Id, cancellationToken: ct);
        Assert.Equal(10, user1Bookings);

        // Проверяем, что у User2 5 броней
        var user2Bookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id, cancellationToken: ct);
        Assert.Equal(5, user2Bookings);

        // User2 создает еще 5 броней (достигает 10)
        for (int i = 0; i < 5; i++)
        {
            await _bookingService.CreateBookingAsync(eventId, user2Id, ct);
        }

        var user2BookingsAfter = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id, cancellationToken: ct);
        Assert.Equal(10, user2BookingsAfter);

        // User1 не может создать 11-ю бронь
        var exception = await Assert.ThrowsAsync<ExceedingActiveBookingLimitException>(() =>
            _bookingService.CreateBookingAsync(eventId, user1Id, ct));

        Assert.Equal($"Не удалось создать бронь, превышен лимит у пользователя с ID {user1Id}", exception.Message);

        // Проверяем, что лимиты независимы
        var user1FinalBookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user1Id, cancellationToken: ct);
        Assert.Equal(10, user1FinalBookings);

        var user2FinalBookings = await _dbContext.Bookings
            .CountAsync(b => b.UserId == user2Id, cancellationToken: ct);
        Assert.Equal(10, user2FinalBookings);

        // Общее количество броней должно быть 20
        var totalBookings = await _dbContext.Bookings
            .CountAsync(cancellationToken: ct);
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


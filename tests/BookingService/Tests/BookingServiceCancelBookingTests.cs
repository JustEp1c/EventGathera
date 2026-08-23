using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Application.Services.Implementations;
using EventGathera.Bookings.Application.Services.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Domain.Exceptions;
using EventGathera.Bookings.Entities.Domain;
using EventGathera.Bookings.Infrastructure.DataAccess;
using EventGathera.Bookings.Infrastructure.Repositories.Implementations;
using EventGathera.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace EventGathera.Tests;

public class BookingServiceCancelBookingTests : IDisposable
{
    private readonly BookingsDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _adminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public BookingServiceCancelBookingTests()
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

    /// <summary>
    /// Тест 1: Успешная отмена брони владельцем
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByOwner_ShouldCancelBookingAndCreateOutboxMessage()
    {
        // Arrange
        var ct = CancellationToken.None;

        var eventId = Guid.NewGuid();

        // Создаем бронь
        var booking = new Booking(eventId, _testUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        // Проверяем начальные условия
        Assert.Equal(BookingStatus.Pending, booking.Status);

        // Act
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User, ct);

        // Assert
        // Проверяем статус брони
        var canceledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancelled, canceledBooking.Status);
        Assert.NotNull(canceledBooking.ProcessedAt);

        // Проверяем, что Outbox сообщение создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCancelled")
            .ToListAsync(ct);
        Assert.Single(outboxMessages);
        Assert.Equal("BookingCancelled", outboxMessages[0].Type);
        Assert.Equal(OutboxStatus.Pending, outboxMessages[0].Status);
    }

    /// <summary>
    /// Тест 2: Успешная отмена брони администратором
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByAdmin_ShouldCancelBookingAndCreateOutboxMessage()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        // Создаем бронь для обычного пользователя
        var booking = new Booking(eventId, anotherUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        // Act - Админ отменяет бронь
        await _bookingService.CancelBookingAsync(booking.Id, _adminUserId, Roles.Admin, ct);

        // Assert
        var canceledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancelled, canceledBooking.Status);
        Assert.NotNull(canceledBooking.ProcessedAt);

        // Проверяем, что Outbox сообщение создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCancelled")
            .ToListAsync(ct);
        Assert.Single(outboxMessages);
        Assert.Equal("BookingCancelled", outboxMessages[0].Type);
    }

    /// <summary>
    /// Тест 3: Попытка отмены чужой брони обычным пользователем - ошибка
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByOtherUser_ShouldThrowForbiddenOperationException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        // Создаем бронь для пользователя
        var booking = new Booking(eventId, ownerUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        // Act & Assert - Другой пользователь пытается отменить бронь
        var anotherUserId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, anotherUserId, Roles.User, ct));

        Assert.Equal($"Невозможно отменить чужую бронь пользователем с ID {anotherUserId}", exception.Message);
        Assert.Equal(anotherUserId, exception.UserId);

        // Проверяем, что бронь не отменилась
        var bookingAfter = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Pending, bookingAfter.Status);
        Assert.Null(bookingAfter.ProcessedAt);

        // Проверяем, что Outbox сообщение НЕ создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCancelled")
            .ToListAsync(ct);
        Assert.Empty(outboxMessages);
    }

    /// <summary>
    /// Тест 4: Попытка отмены уже отмененной брони
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        // Создаем и отменяем бронь
        var booking = new Booking(eventId, _testUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        booking.Cancel();
        await _dbContext.SaveChangesAsync(ct);

        // Act & Assert - Пытаемся отменить еще раз
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User, ct));

        Assert.Equal($"Бронь с ID {booking.Id} уже отменена", exception.Message);

        // Проверяем, что Outbox сообщение НЕ создано (повторно)
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCancelled")
            .ToListAsync(ct);
        Assert.Empty(outboxMessages);
    }

    /// <summary>
    /// Тест 5: Попытка отмены несуществующей брони
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_NonExistentBooking_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var nonExistentBookingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _bookingService.CancelBookingAsync(nonExistentBookingId, _testUserId, Roles.User, ct));

        Assert.Equal($"Бронь с ID {nonExistentBookingId} не найдена", exception.Message);
        Assert.Equal(nonExistentBookingId, exception.ResourceId);

        // Проверяем, что Outbox сообщение НЕ создано
        var outboxMessages = await _dbContext.OutboxMessages
            .Where(o => o.Type == "BookingCancelled")
            .ToListAsync(ct);
        Assert.Empty(outboxMessages);
    }

    /// <summary>
    /// Тест 6: Отмена брони и создание Outbox сообщения с правильными данными
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldCreateOutboxMessageWithCorrectData()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        var booking = new Booking(eventId, _testUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        // Act
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User, ct);

        // Assert
        var outboxMessage = await _dbContext.OutboxMessages
            .FirstOrDefaultAsync(o => o.Type == "BookingCancelled", ct);
        Assert.NotNull(outboxMessage);

        // Проверяем содержимое Outbox сообщения
        var bookingCancelled = JsonSerializer.Deserialize<BookingCancelled>(outboxMessage.Payload);
        Assert.NotNull(bookingCancelled);
        Assert.Equal(booking.Id, bookingCancelled.BookingId);
        Assert.Equal(booking.EventId, bookingCancelled.EventId);
        Assert.Equal(booking.UserId, bookingCancelled.UserId);
        Assert.True(bookingCancelled.CancelledAt <= DateTime.UtcNow);
    }

    /// <summary>
    /// Тест 7: Проверка персистентности - отмена брони сохраняется в БД
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldPersistChangesToDatabase()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        var booking = new Booking(eventId, _testUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        var bookingId = booking.Id;

        // Act
        await _bookingService.CancelBookingAsync(bookingId, _testUserId, Roles.User, ct);

        // Assert - используем новый контекст
        await using var verifyContext = new BookingsDbContext(
            new DbContextOptionsBuilder<BookingsDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);

        // Проверяем бронь в новом контексте
        var canceledBooking = await verifyContext.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        Assert.NotNull(canceledBooking);
        Assert.Equal(BookingStatus.Cancelled, canceledBooking.Status);
        Assert.NotNull(canceledBooking.ProcessedAt);

        // Проверяем Outbox сообщение в новом контексте
        var outboxMessage = await verifyContext.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Type == "BookingCancelled", ct);
        Assert.NotNull(outboxMessage);
        Assert.Equal(OutboxStatus.Pending, outboxMessage.Status);
    }

    /// <summary>
    /// Тест 8: Отмена брони устанавливает ProcessedAt
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldSetProcessedAtTimestamp()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventId = Guid.NewGuid();

        var booking = new Booking(eventId, _testUserId);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync(ct);

        Assert.Null(booking.ProcessedAt);

        // Act
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User, ct);

        // Assert
        var canceledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.NotNull(canceledBooking.ProcessedAt);
        Assert.True(canceledBooking.ProcessedAt <= DateTime.UtcNow);
        Assert.True(canceledBooking.ProcessedAt >= DateTime.UtcNow.AddSeconds(-5));
    }

    public void Dispose()
    {
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
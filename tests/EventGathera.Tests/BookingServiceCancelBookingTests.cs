using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Implementations;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
using EventGathera.Domain.Enums;
using EventGathera.Domain.Exceptions;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Tests;

public class BookingServiceCancelBookingTests
{
    private readonly AppDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _adminUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public BookingServiceCancelBookingTests()
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
    }

    /// <summary>
    /// Тест 1: Успешная отмена брони владельцем
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByOwner_ShouldCancelBookingAndReleaseSeats()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Создаем бронь
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, _testUserId, ct);

        // Проверяем начальные условия
        var eventBefore = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(9, eventBefore.AvailableSeats); // 10 - 1 = 9
        Assert.Equal(BookingStatus.Pending, booking.Status);

        // Act
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User.ToString(), ct);

        // Assert
        // Проверяем статус брони
        var canceledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancel, canceledBooking.Status);
        Assert.NotNull(canceledBooking.ProcessedAt);

        // Проверяем, что места освободились
        var eventAfter = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(10, eventAfter.AvailableSeats); // Места вернулись
    }

    /// <summary>
    /// Тест 2: Успешная отмена брони администратором
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByAdmin_ShouldCancelBookingAndReleaseSeats()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Создаем бронь для обычного пользователя
        var anotherUserId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, anotherUserId, ct);

        // Проверяем начальные условия
        var eventBefore = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(9, eventBefore.AvailableSeats);

        // Act - Админ отменяет бронь
        await _bookingService.CancelBookingAsync(booking.Id, _adminUserId, Roles.Admin.ToString(), ct);

        // Assert
        // Проверяем статус брони
        var canceledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancel, canceledBooking.Status);
        Assert.NotNull(canceledBooking.ProcessedAt);

        // Проверяем, что места освободились
        var eventAfter = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(10, eventAfter.AvailableSeats);
    }

    /// <summary>
    /// Тест 3: Попытка отмены чужой брони обычным пользователем - ошибка
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ByOtherUser_ShouldThrowForbiddenOperationException()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Создаем бронь для пользователя
        var ownerUserId = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, ownerUserId, ct);

        // Act & Assert - Другой пользователь пытается отменить бронь
        var anotherUserId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, anotherUserId, Roles.User.ToString(), ct));

        Assert.Equal($"Невозможно отменить чужую бронь пользователем с ID {anotherUserId}", exception.Message);
        Assert.Equal(anotherUserId, exception.UserId);

        // Проверяем, что бронь не отменилась
        var bookingAfter = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Pending, bookingAfter.Status);
        Assert.Null(bookingAfter.ProcessedAt);

        // Проверяем, что места не освободились
        var eventAfter = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(9, eventAfter.AvailableSeats);
    }

    /// <summary>
    /// Тест 4: Попытка отмены уже отмененной брони
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_AlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Создаем и отменяем бронь
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, _testUserId, ct);
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User.ToString(), ct);

        // Проверяем, что бронь отменена
        var cancelledBooking = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancel, cancelledBooking.Status);

        // Act & Assert - Пытаемся отменить еще раз
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User.ToString(), ct));

        Assert.Equal($"Бронь с ID {booking.Id} уже отменена", exception.Message);

        // Проверяем, что статус не изменился
        var bookingAfter = await _dbContext.Bookings
            .FirstAsync(b => b.Id == booking.Id, cancellationToken: ct);
        Assert.Equal(BookingStatus.Cancel, bookingAfter.Status);
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
            _bookingService.CancelBookingAsync(nonExistentBookingId, _testUserId, Roles.User.ToString(), ct));

        Assert.Equal($"Бронь с ID {nonExistentBookingId} не найдена", exception.Message);
        Assert.Equal(nonExistentBookingId, exception.ResourceId);
    }

    /// <summary>
    /// Тест 6: Отмена брони освобождает места и другие пользователи могут забронировать
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldReleaseSeatsAndAllowOthersToBook()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие с 1 местом
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 1,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Первый пользователь создает бронь
        var user1Id = Guid.NewGuid();
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, user1Id, ct);

        // Проверяем, что мест нет
        var eventBefore = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(0, eventBefore.AvailableSeats);

        // Act - Отменяем бронь
        await _bookingService.CancelBookingAsync(booking.Id, user1Id, Roles.User.ToString(), ct);

        // Проверяем, что место освободилось
        var eventAfterCancel = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(1, eventAfterCancel.AvailableSeats);

        // Другой пользователь может забронировать
        var user2Id = Guid.NewGuid();
        var newBooking = await _bookingService.CreateBookingAsync(eventEntity.Id, user2Id, ct);

        // Проверяем, что бронь создалась
        Assert.NotNull(newBooking);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.Equal(eventEntity.Id, newBooking.EventId);

        // Проверяем, что мест снова нет
        var eventFinal = await _dbContext.Events.FindAsync(eventEntity.Id);
        Assert.Equal(0, eventFinal.AvailableSeats);
    }

    /// <summary>
    /// Тест 7: Отмена брони с обновлением статуса события (если событие было отменено)
    /// </summary>
    [Fact]
    public async Task CancelBookingAsync_ShouldSetProcessedAtTimestamp()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Создаем событие
        var eventEntity = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        );
        _dbContext.Events.Add(eventEntity);
        await _dbContext.SaveChangesAsync(ct);

        // Создаем бронь
        var booking = await _bookingService.CreateBookingAsync(eventEntity.Id, _testUserId, ct);
        Assert.Null(booking.ProcessedAt);

        // Act
        await _bookingService.CancelBookingAsync(booking.Id, _testUserId, Roles.User.ToString(), ct);

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
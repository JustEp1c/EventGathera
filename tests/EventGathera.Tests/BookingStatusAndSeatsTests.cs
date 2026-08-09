using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Implementations;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
using EventGathera.Domain.Enums;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Tests;

public class BookingStatusAndSeatsTests
{
    private readonly AppDbContext _dbContext;
    private readonly IBookingService _bookingService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _existingEventId;

    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BookingStatusAndSeatsTests()
    {
        _dbName = Guid.NewGuid().ToString();
        _existingEventId = Guid.NewGuid();

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
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = _existingEventId
        };
        _dbContext.Events.Add(testEvent);
        _dbContext.SaveChanges();
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(
            _existingEventId,
            _testUserId
        );

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejectedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(
            _existingEventId,
            _testUserId
        );

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReleaseSeats_ShouldRestoreAvailableSeats()
    {
        // Arrange
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventEntity.AvailableSeats;

        // Бронируем 3 места
        eventEntity.TryReserveSeats(3);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        int afterReserveSeats = eventEntity.AvailableSeats;
        Assert.Equal(initialAvailableSeats - 3, afterReserveSeats);

        // Act - освобождаем 2 места
        bool result = eventEntity.ReleaseSeats(2);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        Assert.Equal(initialAvailableSeats - 1, eventEntity.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_ShouldReleaseAllSeats_WhenCalledWithFullCount()
    {
        // Arrange
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventEntity.AvailableSeats;

        eventEntity.TryReserveSeats(initialAvailableSeats);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, eventEntity.AvailableSeats);

        // Act
        bool result = eventEntity.ReleaseSeats(initialAvailableSeats);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        Assert.Equal(initialAvailableSeats, eventEntity.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_ShouldReturnFalse_WhenReleasingMoreThanTaken()
    {
        // Arrange
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventEntity.AvailableSeats;

        eventEntity.TryReserveSeats(3);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(initialAvailableSeats - 3, eventEntity.AvailableSeats);

        // Act
        bool result = eventEntity.ReleaseSeats(5);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
        Assert.Equal(initialAvailableSeats - 3, eventEntity.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_ShouldAllowNewBookingOnSameSeat()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventEntity.AvailableSeats;

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(initialAvailableSeats - 1, eventEntity.AvailableSeats);

        booking1.Reject();
        eventEntity.ReleaseSeats(1);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(initialAvailableSeats, eventEntity.AvailableSeats);

        // Act
        var booking2 = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(booking2);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(initialAvailableSeats - 1, eventEntity.AvailableSeats);
        Assert.Equal(BookingStatus.Pending, booking2.Status);
    }

    [Fact]
    public async Task ReleaseSeats_WithMultipleBookings_ShouldAllowNewBookingsUpToCapacity()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int totalSeats = eventEntity.TotalSeats;

        // Act
        var bookings = new List<Booking>();
        for (int i = 0; i < totalSeats; i++)
        {
            var booking = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
            bookings.Add(booking);
        }

        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, eventEntity.AvailableSeats);

        bookings[0].Reject();
        eventEntity.ReleaseSeats(1);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, eventEntity.AvailableSeats);

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(newBooking);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.Equal(0, eventEntity.AvailableSeats);

        Assert.DoesNotContain(bookings, b => b.Id == newBooking.Id);
    }

    [Fact]
    public async Task ReleaseSeats_WhenBookingRejected_ShouldRestoreSeatsAndAllowNewBooking()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        int initialAvailableSeats = eventEntity.AvailableSeats;

        var booking = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(initialAvailableSeats - 1, eventEntity.AvailableSeats);

        booking.Reject();
        bool releaseResult = eventEntity.ReleaseSeats(1);
        Assert.True(releaseResult);
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(initialAvailableSeats, eventEntity.AvailableSeats);

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(_existingEventId, _testUserId, ct);
        await _dbContext.Entry(eventEntity).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(newBooking);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.Equal(initialAvailableSeats - 1, eventEntity.AvailableSeats);
        Assert.NotEqual(booking.Id, newBooking.Id);
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
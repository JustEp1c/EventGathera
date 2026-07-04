using EventGathera.Presentation.Domain;
using EventGathera.Presentation.Contracts.Enums;
using EventGathera.Presentation.DataAccess;
using EventGathera.Presentation.Exceptions;
using EventGathera.Presentation.Repositories.Implementations;
using EventGathera.Presentation.Repositories.Interfaces;
using EventGathera.Presentation.Services.Implementations;
using EventGathera.Presentation.Services.Interfaces;
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
        var result = await _bookingService.CreateBookingAsync(_existingEventId, ct);

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
        var result1 = await _bookingService.CreateBookingAsync(_existingEventId, ct);
        var result2 = await _bookingService.CreateBookingAsync(_existingEventId, ct);

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
            _bookingService.CreateBookingAsync(nonExistingEventId, ct));

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
            _bookingService.CreateBookingAsync(_existingEventId, ct));

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
        var booking = await _bookingService.CreateBookingAsync(_existingEventId, ct);

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
            var booking = await _bookingService.CreateBookingAsync(_existingEventId, ct);
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
            await _bookingService.CreateBookingAsync(_existingEventId, ct);
        }

        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, eventAfter.AvailableSeats);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, ct));

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
            await _bookingService.CreateBookingAsync(_existingEventId, ct);
        }

        int bookingsCountBefore = await _dbContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, ct));

        // Assert
        int bookingsCountAfter = await _dbContext.Bookings.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(bookingsCountBefore, bookingsCountAfter);

        var eventAfter = await _dbContext.Events
            .FirstAsync(e => e.Id == _existingEventId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(0, eventAfter.AvailableSeats);
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


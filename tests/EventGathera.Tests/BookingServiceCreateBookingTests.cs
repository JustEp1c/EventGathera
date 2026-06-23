using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Tests;

public class BookingServiceCreateBookingTests
{
    private readonly BookingService _bookingService;
    private readonly BookingStorage _bookingStorage;
    private readonly EventStorage _eventStorage;
    private readonly Guid _existingEventId;
    private readonly int _initialTotalSeats = 100;

    public BookingServiceCreateBookingTests()
    {
        _bookingStorage = new BookingStorage();
        _eventStorage = new EventStorage();

        _existingEventId = Guid.NewGuid();

        _eventStorage.Events.Add(new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: _initialTotalSeats,
            description: "Test Description"
        )
        {
            Id = _existingEventId
        });

        var eventService = new EventService(_eventStorage);
        _bookingService = new BookingService(_bookingStorage, eventService);
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
        _eventStorage.Events.RemoveAt(0);

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

        var eventBefore = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = eventBefore.AvailableSeats;

        // Act
        var booking = await _bookingService.CreateBookingAsync(_existingEventId, ct);

        // Assert
        var eventAfter = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(initialAvailableSeats - 1, eventAfter.AvailableSeats);
        Assert.Equal(booking.EventId, _existingEventId);
    }

    [Fact]
    public async Task CreateMultipleBookings_UpToLimit_ShouldAllSucceedAndHaveUniqueIds()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = _eventStorage.Events.First(e => e.Id == _existingEventId);
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

        var eventAfter = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var ct = CancellationToken.None;
        var eventEntity = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int totalSeats = eventEntity.TotalSeats;

        for (int i = 0; i < totalSeats; i++)
        {
            await _bookingService.CreateBookingAsync(_existingEventId, ct);
        }

        var eventAfter = _eventStorage.Events.First(e => e.Id == _existingEventId);
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
        var eventEntity = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int totalSeats = eventEntity.TotalSeats;

        for (int i = 0; i < totalSeats; i++)
        {
            await _bookingService.CreateBookingAsync(_existingEventId, ct);
        }

        int bookingsCountBefore = _bookingStorage.Bookings.Count;

        // Act & Assert
        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            _bookingService.CreateBookingAsync(_existingEventId, ct));

        // Assert
        Assert.Equal(bookingsCountBefore, _bookingStorage.Bookings.Count);

        var eventAfter = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }
}


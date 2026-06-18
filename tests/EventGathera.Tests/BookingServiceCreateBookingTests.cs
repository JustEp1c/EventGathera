using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;

namespace EventGathera.Tests;

public class BookingServiceCreateBookingTests
{
    private readonly BookingService _bookingService;
    private readonly BookingStorage _bookingStorage;
    private readonly EventStorage _eventStorage;
    private readonly Guid _existingEventId;

    public BookingServiceCreateBookingTests()
    {
        _bookingStorage = new BookingStorage();
        _eventStorage = new EventStorage();

        _existingEventId = Guid.NewGuid();

        _eventStorage.Events.Add(new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 100,
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
}


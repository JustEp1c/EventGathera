using EventGathera.Api.BackgroundServices;
using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using Microsoft.Extensions.Logging;
using Microsoft.Testing.Platform.Logging;
using Moq;

namespace EventGathera.Tests;

public class BookingServiceGetBookingByIdTests
{
    private readonly BookingService _bookingService;
    private readonly BookingStorage _bookingStorage;
    private readonly EventStorage _eventStorage;
    private readonly Guid _existingBookingId;
    private readonly Guid _existingEventId;

    public BookingServiceGetBookingByIdTests()
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

        _existingBookingId = Guid.NewGuid();

        _bookingStorage.Bookings.Add(new Booking
        {
            Id = _existingBookingId,
            EventId = _existingEventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });

        var eventService = new EventService(_eventStorage);
        _bookingService = new BookingService(_bookingStorage, eventService);
    }

    [Fact]
    public async Task GetBookingById_WithValidBookingId_ShouldReturnFoundBooking()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result = await _bookingService.GetBookingByIdAsync(_existingBookingId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_existingEventId, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetBookingById_WithValidBookingId_ShouldReturnDifferentStatuses()
    {
        // Arrange
        var ct = CancellationToken.None;
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<BookingProcessingService>>();

        var beforeStatus = _bookingStorage.Bookings.First();
        Assert.Equal(BookingStatus.Pending, beforeStatus.Status);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var backgroundService = new BookingProcessingService(_bookingStorage, _eventStorage, mockLogger.Object);
        var backgroundTask = backgroundService.StartAsync(cts.Token);

        await Task.Delay(TimeSpan.FromSeconds(6), ct);

        var afterStatus = await _bookingService.GetBookingByIdAsync(beforeStatus.Id, ct);

        // Assert
        Assert.Equal(BookingStatus.Confirmed, afterStatus.Status);
        Assert.NotNull(afterStatus.ProcessedAt);
        Assert.Equal(beforeStatus.Status, afterStatus.Status);

        await backgroundService.StopAsync(cts.Token);
    }

    [Fact]
    public async Task GetBookingById_WithNonExistingBookingId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        Guid nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(nonExistingId, ct));

        Assert.Equal($"Бронь с ID {nonExistingId} не найдена", exception.Message);
    }
}

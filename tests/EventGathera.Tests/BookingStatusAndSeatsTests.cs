using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Tests;

public class BookingStatusAndSeatsTests
{
    private readonly BookingStorage _bookingStorage;
    private readonly EventStorage _eventStorage;
    private readonly BookingService _bookingService;
    private readonly Guid _existingEventId;

    public BookingStatusAndSeatsTests()
    {
        _bookingStorage = new BookingStorage();
        _eventStorage = new EventStorage();

        _existingEventId = Guid.NewGuid();

        _eventStorage.Events.Add(new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 10,
            description: "Test Description"
        )
        {
            Id = _existingEventId
        });

        var eventService = new EventService(_eventStorage);
        _bookingService = new BookingService(_bookingStorage, eventService);
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = _existingEventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

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
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = _existingEventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void ReleaseSeats_ShouldRestoreAvailableSeats()
    {
        // Arrange
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = @event.AvailableSeats;

        // Бронируем 3 места
        @event.TryReserveSeats(3);
        int afterReserveSeats = @event.AvailableSeats;
        Assert.Equal(initialAvailableSeats - 3, afterReserveSeats);

        // Act - освобождаем 2 места
        bool result = @event.ReleaseSeats(2);

        // Assert
        Assert.True(result);
        Assert.Equal(initialAvailableSeats - 1, @event.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_ShouldReleaseAllSeats_WhenCalledWithFullCount()
    {
        // Arrange
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = @event.AvailableSeats;

        @event.TryReserveSeats(initialAvailableSeats);
        Assert.Equal(0, @event.AvailableSeats);

        // Act
        bool result = @event.ReleaseSeats(initialAvailableSeats);

        // Assert
        Assert.True(result);
        Assert.Equal(initialAvailableSeats, @event.AvailableSeats);
    }

    [Fact]
    public void ReleaseSeats_ShouldReturnFalse_WhenReleasingMoreThanTaken()
    {
        // Arrange
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = @event.AvailableSeats;

        @event.TryReserveSeats(3);
        Assert.Equal(initialAvailableSeats - 3, @event.AvailableSeats);

        // Act
        bool result = @event.ReleaseSeats(5);

        // Assert
        Assert.False(result);
        Assert.Equal(initialAvailableSeats - 3, @event.AvailableSeats);
    }

    [Fact]
    public async Task ReleaseSeats_ShouldAllowNewBookingOnSameSeat()
    {
        // Arrange
        var ct = CancellationToken.None;
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = @event.AvailableSeats;

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(_existingEventId, ct);
        var afterFirstBooking = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(initialAvailableSeats - 1, afterFirstBooking.AvailableSeats);

        booking1.Reject();
        @event.ReleaseSeats(1);
        var afterRelease = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(initialAvailableSeats, afterRelease.AvailableSeats);

        // Act
        var booking2 = await _bookingService.CreateBookingAsync(_existingEventId, ct);
        var afterSecondBooking = _eventStorage.Events.First(e => e.Id == _existingEventId);

        // Assert
        Assert.NotNull(booking2);
        Assert.NotEqual(booking1.Id, booking2.Id);
        Assert.Equal(initialAvailableSeats - 1, afterSecondBooking.AvailableSeats);
        Assert.Equal(BookingStatus.Pending, booking2.Status);
    }

    [Fact]
    public async Task ReleaseSeats_WithMultipleBookings_ShouldAllowNewBookingsUpToCapacity()
    {
        // Arrange
        var ct = CancellationToken.None;
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int totalSeats = @event.TotalSeats;

        // Act
        var bookings = new List<Booking>();
        for (int i = 0; i < totalSeats; i++)
        {
            var booking = await _bookingService.CreateBookingAsync(_existingEventId, ct);
            bookings.Add(booking);
        }

        Assert.Equal(0, _eventStorage.Events.First(e => e.Id == _existingEventId).AvailableSeats);

        bookings[0].Reject();
        @event.ReleaseSeats(1);

        var afterRelease = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(1, afterRelease.AvailableSeats);

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(_existingEventId, ct);

        // Assert
        Assert.NotNull(newBooking);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.Equal(0, _eventStorage.Events.First(e => e.Id == _existingEventId).AvailableSeats);

        Assert.DoesNotContain(bookings, b => b.Id == newBooking.Id);
    }

    [Fact]
    public async Task ReleaseSeats_WhenBookingRejected_ShouldRestoreSeatsAndAllowNewBooking()
    {
        // Arrange
        var ct = CancellationToken.None;
        var @event = _eventStorage.Events.First(e => e.Id == _existingEventId);
        int initialAvailableSeats = @event.AvailableSeats;

        var booking = await _bookingService.CreateBookingAsync(_existingEventId, ct);
        var afterCreate = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(initialAvailableSeats - 1, afterCreate.AvailableSeats);

        booking.Reject();
        bool releaseResult = @event.ReleaseSeats(1);
        Assert.True(releaseResult);

        var afterReject = _eventStorage.Events.First(e => e.Id == _existingEventId);
        Assert.Equal(initialAvailableSeats, afterReject.AvailableSeats);

        // Act
        var newBooking = await _bookingService.CreateBookingAsync(_existingEventId, ct);
        var afterNewBooking = _eventStorage.Events.First(e => e.Id == _existingEventId);

        // Assert
        Assert.NotNull(newBooking);
        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.Equal(initialAvailableSeats - 1, afterNewBooking.AvailableSeats);
        Assert.NotEqual(booking.Id, newBooking.Id);
    }

    [Fact]
    public async Task ConcurrentBookings_WithLimitedSeats_ShouldPreventOverbooking()
    {
        // Arrange
        var ct = CancellationToken.None;
        const int totalSeats = 5;
        const int totalRequests = 20;

        // Создаем новое событие с 5 местами специально для этого теста
        var limitedEventId = Guid.NewGuid();
        _eventStorage.Events.Add(new Event(
            title: "Limited Event for Concurrency Test",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: totalSeats,
            description: "Event for overbooking test"
        )
        {
            Id = limitedEventId
        });

        var tasks = new List<Task<Booking>>();
        var exceptions = new List<Exception>();
        var successfulBookings = new List<Booking>();

        // Act
        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                return await _bookingService.CreateBookingAsync(limitedEventId, ct);
            }));
        }

        foreach (var task in tasks)
        {
            try
            {
                var booking = await task;
                successfulBookings.Add(booking);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // Assert
        Assert.Equal(totalSeats, successfulBookings.Count);

        var uniqueIds = successfulBookings.Select(b => b.Id).Distinct().Count();
        Assert.Equal(totalSeats, uniqueIds);

        var noSeatsExceptions = exceptions.OfType<NoAvailableSeatsException>().ToList();
        Assert.Equal(totalRequests - totalSeats, noSeatsExceptions.Count);
        foreach (var ex in noSeatsExceptions)
        {
            Assert.Equal("Нет свободных мест на это событие", ex.Message);
        }

        var eventAfter = _eventStorage.Events.First(e => e.Id == limitedEventId);
        Assert.Equal(0, eventAfter.AvailableSeats);
    }

    [Fact]
    public async Task ConcurrentBookings_WithAllSeatsFilled_ShouldHaveUniqueIdsForAllBookings()
    {
        // Arrange
        var ct = CancellationToken.None;
        const int totalSeats = 10;

        // Создаем новое событие с 10 местами специально для этого теста
        var limitedEventId = Guid.NewGuid();
        _eventStorage.Events.Add(new Event(
            title: "Event for Unique IDs Test",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: totalSeats,
            description: "Event for unique ID concurrency test"
        )
        {
            Id = limitedEventId
        });

        var tasks = new List<Task<Booking>>();
        var exceptions = new List<Exception>();
        var successfulBookings = new List<Booking>();

        // Act
        for (int i = 0; i < totalSeats; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                return await _bookingService.CreateBookingAsync(limitedEventId, ct);
            }));
        }

        foreach (var task in tasks)
        {
            try
            {
                var booking = await task;
                successfulBookings.Add(booking);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // Assert
        Assert.Equal(totalSeats, successfulBookings.Count);

        var allIds = successfulBookings.Select(b => b.Id).ToList();
        var uniqueIds = allIds.Distinct().ToList();
        Assert.Equal(totalSeats, uniqueIds.Count);
        Assert.Equal(allIds.Count, uniqueIds.Count);

        var eventAfter = _eventStorage.Events.First(e => e.Id == limitedEventId);
        Assert.Equal(0, eventAfter.AvailableSeats);

        var bookingsInStorage = _bookingStorage.Bookings
            .Where(b => b.EventId == limitedEventId)
            .ToList();
        Assert.Equal(totalSeats, bookingsInStorage.Count);

        Assert.Empty(exceptions);
    }
}
using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Interfaces;

namespace EventGathera.Api.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly BookingStorage _bookingStorage;

    private readonly IEventService _eventService;

    private readonly object _bookingLock = new();

    public BookingService(BookingStorage bookingStorage, IEventService eventService)
    {
        _bookingStorage = bookingStorage;
        _eventService = eventService;
    }

    /// <inheritdoc/>
    public Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var foundEvent = _eventService.GetEventById(eventId);

        Booking newBooking = null!;

        lock (_bookingLock)
        {

            if (!foundEvent.TryReserveSeats())
            {

                throw new NoAvailableSeatsException("Нет свободных мест на это событие");

            }

            newBooking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = foundEvent.Id,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            _bookingStorage.Bookings.Add(newBooking);

        }

        return Task.FromResult(newBooking);
    }

    /// <inheritdoc/>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var foundBooking = _bookingStorage.Bookings.Find(b => b.Id == bookingId);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена", bookingId);
        }

        return Task.FromResult(foundBooking);
    }
}

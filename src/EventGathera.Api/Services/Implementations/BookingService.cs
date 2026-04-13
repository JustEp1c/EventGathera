using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Interfaces;

namespace EventGathera.Api.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly BookingStorage _storage;

    public BookingService(BookingStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        var newBooking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        _storage.Bookings.Add(newBooking);

        return Task.FromResult(newBooking);
    }

    /// <inheritdoc/>
    public Task<Booking> GetBookingByIdAsync(Guid bookingId)
    {
        var foundBooking = _storage.Bookings.Find(b => b.Id == bookingId);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена");
        }

        return Task.FromResult(foundBooking);
    }
}

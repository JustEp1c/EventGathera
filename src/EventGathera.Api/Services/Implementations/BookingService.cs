using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Api.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly AppDbContext _appDbContext;

    public BookingService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await BookingLock.WaitAsync(ct);

        try
        {
            var foundEvent = await _appDbContext.Events
                .FirstOrDefaultAsync(e => e.Id == eventId, ct);

            if (foundEvent is null)
            {
                throw new ResourceNotFoundException($"Событие с ID {eventId} не найдено", eventId);
            }


            if (!foundEvent.TryReserveSeats())
            {

                throw new NoAvailableSeatsException("Нет свободных мест на это событие");

            }

            _appDbContext.Events.Update(foundEvent);

            var newBooking = new Booking(
                foundEvent.Id
            );

            await _appDbContext.Bookings.AddAsync(newBooking, ct);

            await _appDbContext.SaveChangesAsync(ct);

            return newBooking;

        }
        finally 
        { 
            BookingLock.Release(); 
        }
    }

    /// <inheritdoc/>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var foundBooking = await _appDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена", bookingId);
        }

        return foundBooking;
    }
}

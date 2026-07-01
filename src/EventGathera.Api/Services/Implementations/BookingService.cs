using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Api.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly AppDbContext _appDbContext;

    public BookingService(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var transaction = await _appDbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var foundEvent = await _appDbContext.Events
                .FromSqlRaw("SELECT * FROM events WHERE id = {0} FOR UPDATE", eventId)
                .FirstOrDefaultAsync(ct);

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

            await transaction.CommitAsync(ct);

            return newBooking;

        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Количество мест было изменено другим пользователем. Попробуйте еще раз.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
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

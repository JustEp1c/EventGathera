using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Application.Services.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Domain.Exceptions;
using EventGathera.Bookings.Entities.Domain;

namespace EventGathera.Bookings.Application.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, Roles role, CancellationToken ct = default)
    {
        var foundBooking = await _bookingRepository.GetBookingByIdAsync(bookingId, ct);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена", bookingId);
        }

        if (foundBooking.Status == BookingStatus.Cancelled)
        {
            throw new InvalidOperationException($"Бронь с ID {bookingId} уже отменена");
        }

        //TODO: отправка сообщения на отмену

        if (role == Roles.Admin || foundBooking.UserId == userId)
        {
            foundBooking.Cancel();

            await _bookingRepository.SaveChangesAsync(ct);
        }
        else
        {
            throw new ForbiddenOperationException($"Невозможно отменить чужую бронь пользователем с ID {userId}", userId);
        }
    }

    /// <inheritdoc/>
    public async Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await BookingLock.WaitAsync(ct);

        try
        {
            var activeBookingsCount = await _bookingRepository.GetActiveBookingsCountByUserAsync(userId, ct);

            if (activeBookingsCount >= 10)
            {
                throw new ExceedingActiveBookingLimitException($"Не удалось создать бронь, превышен лимит у пользователя с ID {userId}", userId);
            }

            var newBooking = new Booking(
                eventId,
                userId
            );

            await _bookingRepository.AddBookingAsync(newBooking, ct);

            await _bookingRepository.SaveChangesAsync(ct);

            return newBooking;

        }
        finally 
        { 
            BookingLock.Release(); 
        }
    }

    /// <inheritdoc/>
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, Guid userId, Roles role, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var foundBooking = await _bookingRepository.GetBookingByIdAsync(bookingId, ct);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена", bookingId);
        }

        if (role != Roles.Admin && foundBooking.UserId != userId)
        {
            throw new ForbiddenOperationException($"Невозможно получить чужую бронь пользователем с ID {userId}", userId);
        }

        return foundBooking;
    }
}

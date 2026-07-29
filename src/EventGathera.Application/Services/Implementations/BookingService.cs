using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
using EventGathera.Domain.Enums;
using EventGathera.Domain.Exceptions;

namespace EventGathera.Application.Services.Implementations;

/// <inheritdoc/>
public class BookingService : IBookingService
{
    private readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookingRepository;

    private readonly IEventRepository _eventRepository;

    public BookingService(IBookingRepository bookingRepository, IEventRepository eventRepository)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId, Roles role, CancellationToken ct = default)
    {
        var foundBooking = await GetBookingByIdAsync(bookingId, ct);

        if (role == Roles.Admin || foundBooking.UserId == userId)
        {
            foundBooking.Cancel();
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

            var foundEvent = await _eventRepository.GetEventByIdAsync(eventId);

            if (foundEvent is null)
            {
                throw new ResourceNotFoundException($"Событие с ID {eventId} не найдено", eventId);
            }

            if (foundEvent.StartAt <= DateTime.UtcNow)
            {
                throw new ExpiredEventBookingException($"Событие с ID {eventId} уже началось", eventId);
            }


            if (!foundEvent.TryReserveSeats())
            {

                throw new NoAvailableSeatsException("Нет свободных мест на это событие");

            }

            var newBooking = new Booking(
                foundEvent.Id,
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
    public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var foundBooking = await _bookingRepository.GetBookingByIdAsync(bookingId, ct);

        if (foundBooking is null)
        {
            throw new ResourceNotFoundException($"Бронь с ID {bookingId} не найдена", bookingId);
        }

        return foundBooking;
    }
}

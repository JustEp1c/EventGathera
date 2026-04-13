using EventGathera.Api.Domain;

namespace EventGathera.Api.Services.Interfaces;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId"></param>
    /// <returns></returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId);
}

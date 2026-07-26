using EventGathera.Domain;

namespace EventGathera.Application.Services.Interfaces;

/// <summary>
/// Сервис для управления бронированиями
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Создание брони для указанного события
    /// </summary>
    /// <param name="eventId">Id события</param>
    /// <param name="ct">Id пользователя</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId">Id бронирования</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns></returns>
    Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
}

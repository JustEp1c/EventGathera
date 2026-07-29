using EventGathera.Domain;
using EventGathera.Domain.Enums;

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

    /// <summary>
    /// Отменить бронь
    /// </summary>
    /// <param name="bookingId">Id брони</param>
    /// <param name="userId">Id пользователя</param>
    /// <param name="role">Роль</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task CancelBookingAsync(Guid bookingId, Guid userId, Roles role, CancellationToken ct = default);
}

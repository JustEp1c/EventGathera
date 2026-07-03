using EventGathera.Api.Domain;

namespace EventGathera.Api.Repositories.Interfaces;

/// <summary>
/// Репозиторий для работы с данными Booking
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Получение всх бронирований с возможностью применения фильтров
    /// </summary>
    /// <returns></returns>
    IQueryable<Booking> GetAllBookingsQuery();

    /// <summary>
    /// Получить бронь по id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Добавить бронирование
    /// </summary>
    /// <param name="booking"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task AddBookingAsync(Booking booking, CancellationToken ct = default);

    /// <summary>
    /// Сохранить изменения
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task SaveChangesAsync(CancellationToken ct = default);
}

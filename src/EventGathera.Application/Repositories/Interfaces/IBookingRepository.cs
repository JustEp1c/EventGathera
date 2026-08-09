using EventGathera.Domain;

namespace EventGathera.Application.Repositories.Interfaces;

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
    /// Получить число активных броней у пользователя
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<int> GetActiveBookingsCountByUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Сохранить изменения
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task SaveChangesAsync(CancellationToken ct = default);
}

using EventGathera.Events.Domain.Entities;

namespace EventGathera.Events.Application.Repositories.Interfaces;

/// <summary>
/// Репозиторий для работы с данными Event
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Получение всех событий с возможностью применения фильтров
    /// </summary>
    IQueryable<Event> GetAllEventsQuery();

    /// <summary>
    /// Получить событие по id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Добавить событие
    /// </summary>
    /// <param name="event"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task AddEventAsync(Event @event, CancellationToken ct = default);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="event"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    void RemoveEvent(Event @event, CancellationToken ct = default);

    /// <summary>
    /// Получить топ-10 событий с наибольшим процентом проданных мест
    /// </summary>
    /// <param name="ct"></param>
    /// <param name="count"></param>
    /// <returns>топ-10 событий</returns>
    Task<List<Event>> GetTopEventsAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Сохранить изменения
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task SaveChangesAsync(CancellationToken ct = default);
}

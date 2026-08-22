using EventGathera.Events.Domain.Entities;

namespace EventGathera.Events.Application.Cache;

/// <summary>
/// Сервис для кеширования событий
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Получить событие по id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Event?> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Положить событие в кеш
    /// </summary>
    /// <param name="event"></param>
    /// <param name="ttl"></param>
    /// <returns></returns>
    Task SetEventAsync(Event @event, int ttl);

    /// <summary>
    /// Удалить событие из кеша по id
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task RemoveEventByIdAsync(Guid id);
}

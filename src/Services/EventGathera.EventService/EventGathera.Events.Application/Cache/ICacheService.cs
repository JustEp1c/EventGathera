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
    /// Сохранить событие в кеш
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

    /// <summary>
    /// Получить топ событий из кеша
    /// </summary>
    /// <param name="topCount"></param>
    /// <returns></returns>
    Task<List<Event>?> GetTopEvents(int topCount);

    /// <summary>
    /// Сохранить топ событий в кеш
    /// </summary>
    /// <param name="top"></param>
    /// <param name="topCount"></param>
    /// <param name="topEventsTTL"></param>
    /// <returns></returns>
    Task SetTopEvents(List<Event> top, int topCount, int topEventsTTL);
}

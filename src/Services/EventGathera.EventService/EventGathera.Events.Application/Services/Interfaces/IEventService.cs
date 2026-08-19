using EventGathera.Events.Application.DTO.Requests;
using EventGathera.Events.Application.DTO.Responses;
using EventGathera.Events.Domain.Entities;

namespace EventGathera.Events.Application.Services.Interfaces;

/// <summary>
/// Сервис для управления событиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить пагинированный список всех событий
    /// </summary>
    /// <param name="queryParams">Параметры запроса для событий</param>
    /// <param name="ct"></param>
    /// <returns>Пагинированный список событий</returns>
    Task<PaginatedResult<Event>> GetAllEventsAsync(EventQueryParams queryParams, CancellationToken ct = default);

    /// <summary>
    /// Получить событие по id
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="ct"></param>
    /// <returns>Событие</returns>
    Task<Event> GetEventByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Создать событие
    /// </summary>
    /// <param name="request">DTO для создания события</param>
    /// <param name="ct"></param>
    /// <returns>Созданное событие</returns>
    Task<Event> CreateEventAsync(EventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Обновить событие целиком
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="request">DTO для обновления события</param>
    /// <param name="ct"></param>
    Task UpdateEventAsync(Guid id, EventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="ct"></param>
    Task DeleteEventAsync(Guid id, CancellationToken ct = default);

}
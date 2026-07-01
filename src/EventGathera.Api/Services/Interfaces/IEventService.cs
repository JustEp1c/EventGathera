using EventGathera.Api.Contracts.DTO.Requests;
using EventGathera.Api.Contracts.DTO.Responses;
using EventGathera.Api.Domain;

namespace EventGathera.Api.Services.Interfaces;

/// <summary>
/// Сервис для управления событиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить пагинированный список всех событий
    /// </summary>
    /// <param name="queryParams">Параметры запроса для событий</param>
    /// <returns>Пагинированный список событий</returns>
    Task<PaginatedResult<Event>> GetAllEventsAsync(EventQueryParams queryParams);

    /// <summary>
    /// Получить событие по id
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <returns>Событие</returns>
    Task<Event> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Создать событие
    /// </summary>
    /// <param name="request">DTO для создания события</param>
    /// <returns>Созданное событие</returns>
    Task<Event> CreateEventAsync(EventRequest request);

    /// <summary>
    /// Обновить событие целиком
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="request">DTO для обновления события</param>
    Task UpdateEventAsync(Guid id, EventRequest request);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    Task DeleteEventAsync(Guid id);

}

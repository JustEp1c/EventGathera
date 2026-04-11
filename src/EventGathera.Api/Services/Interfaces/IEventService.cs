using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.DTO.Responses;

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
    PaginatedResult<Event> GetAllEvents(EventQueryParams queryParams);

    /// <summary>
    /// Получить событие по id
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <returns>Событие</returns>
    Event GetEventById(int id);

    /// <summary>
    /// Создать событие
    /// </summary>
    /// <param name="request">DTO для создания события</param>
    /// <returns>Созданное событие</returns>
    Event CreateEvent(EventRequest request);

    /// <summary>
    /// Обновить событие целиком
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    /// <param name="request">DTO для обновления события</param>
    void UpdateEvent(int id, EventRequest request);

    /// <summary>
    /// Удалить событие
    /// </summary>
    /// <param name="id">Уникальный идентификатор</param>
    void DeleteEvent(int id);

}

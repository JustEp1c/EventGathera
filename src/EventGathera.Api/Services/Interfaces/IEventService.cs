using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;

namespace EventGathera.Api.Services.Interfaces;

/// <summary>
/// Сервис для управления событиями
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Получить все события
    /// </summary>
    /// <param name="title">название события</param>
    /// <param name="from">Дата начала события</param>
    /// <param name="to">Дата окончания события</param>
    /// <returns>Список событий</returns>
    List<Event> GetAllEvents(string? title, DateTime? from, DateTime? to);

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

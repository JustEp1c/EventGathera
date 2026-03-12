namespace EventGathera.Api.Domain;


/// <summary>
/// Доменная сущность Event (Событие)
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Название
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Время начала события
    /// </summary>
    public required DateTime StartAt { get; set; }

    /// <summary>
    /// Время окончания события
    /// </summary>
    public required DateTime EndAt { get; set; }
}

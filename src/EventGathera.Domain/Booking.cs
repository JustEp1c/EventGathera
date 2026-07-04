using EventGathera.Domain.Enums;

namespace EventGathera.Domain;

/// <summary>
/// Доменная сущность Booking (бронь)
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата время создания брони
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Навигационное свойство для связи с событием
    /// </summary>
    public Event Event { get; set; }

    private Booking()
    {

    }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        CreatedAt = DateTime.UtcNow;
        Status = BookingStatus.Pending;
    }

    /// <summary>
    /// Подтверждение брони
    /// </summary>
    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отклонение брони
    /// </summary>
    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }
}

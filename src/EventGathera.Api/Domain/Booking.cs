using EventGathera.Api.Contracts.Enums;

namespace EventGathera.Api.Domain;

/// <summary>
/// Доменная сущность Booking (бронь)
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public required Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public required Guid EventId { get; init; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>
    /// Дата время создания брони
    /// </summary>
    public required DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>
    /// Дата и время обработки брони
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}

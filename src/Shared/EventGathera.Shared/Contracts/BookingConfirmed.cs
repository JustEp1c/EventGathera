namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: бронь подтверждена (опционально для Event Service)
/// </summary>
public record BookingConfirmed
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime ConfirmedAt { get; init; }
}

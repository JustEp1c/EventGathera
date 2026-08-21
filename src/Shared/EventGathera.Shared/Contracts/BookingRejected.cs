namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: бронь отклонена (опционально для Event Service)
/// </summary>
public record BookingRejected
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime RejectedAt { get; init; }
}

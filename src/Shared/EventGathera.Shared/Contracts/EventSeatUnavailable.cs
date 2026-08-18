namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: места не могут быть зарезервированы
/// </summary>
public record EventSeatUnavailable
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime FailedAt { get; init; }
}
namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: бронь отменена
/// </summary>
public record BookingCancelled
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CancelledAt { get; init; }
}


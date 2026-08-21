namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: бронь создана в сервисе Bookings
/// </summary>
public record BookingCreated
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public DateTime CreatedAt { get; init; }
}

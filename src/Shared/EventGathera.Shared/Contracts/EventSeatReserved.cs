namespace EventGathera.Shared.Contracts;

/// <summary>
/// Событие: места успешно зарезервированы в Events Service
/// </summary>
public record EventSeatReserved
{
    public Guid BookingId { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int SeatsReserved { get; init; }
    public int AvailableSeats { get; init; }
    public DateTime ReservedAt { get; init; }
}

namespace EventGathera.Bookings.Domain.Enums;

/// <summary>
/// Статусы обработку сообщений в outbox
/// </summary>
public enum OutboxStatus
{
    Pending,
    Published,
    Failed
}

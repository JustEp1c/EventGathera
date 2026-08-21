namespace EventGathera.Events.Domain.Enums;

/// <summary>
/// Статусы обработку сообщений в outbox
/// </summary>
public enum OutboxStatus
{
    Pending,
    Published,
    Failed
}

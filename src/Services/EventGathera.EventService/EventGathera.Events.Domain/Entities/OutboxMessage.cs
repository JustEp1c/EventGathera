using EventGathera.Events.Domain.Enums;

namespace EventGathera.Events.Domain.Entities;

/// <summary>
/// Сущность для реализации Transactional Outbox
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public OutboxStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
        Status = OutboxStatus.Pending;
        RetryCount = 0;
    }

    public void MarkAsPublished()
    {
        Status = OutboxStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        Status = OutboxStatus.Failed;
        ErrorMessage = error;
        RetryCount++;
    }

    public void IncrementRetry()
    {
        RetryCount++;
    }
}
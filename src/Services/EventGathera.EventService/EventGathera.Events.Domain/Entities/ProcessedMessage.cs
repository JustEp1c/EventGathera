namespace EventGathera.Events.Domain.Entities;

public class ProcessedMessage
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = string.Empty;
    public string MessageType { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedMessage() { }

    public ProcessedMessage(string messageId, string messageType)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        MessageType = messageType;
        ProcessedAt = DateTime.UtcNow;
    }
}

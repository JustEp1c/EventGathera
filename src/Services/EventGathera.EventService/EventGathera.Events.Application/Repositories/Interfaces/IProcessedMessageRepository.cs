using EventGathera.Events.Domain.Entities;

namespace EventGathera.Events.Application.Repositories.Interfaces;

public interface IProcessedMessageRepository
{
    Task<bool> ExistsAsync(string messageId, string messageType, CancellationToken ct = default);
    Task AddAsync(ProcessedMessage message, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
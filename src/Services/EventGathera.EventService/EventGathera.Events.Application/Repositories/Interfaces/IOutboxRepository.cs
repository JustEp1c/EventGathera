using EventGathera.Events.Domain.Entities;

namespace EventGathera.Events.Application.Repositories.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken ct = default);
    Task MarkAsPublishedAsync(Guid id, CancellationToken ct = default);
    Task MarkAsFailedAsync(Guid id, string error, CancellationToken ct = default);
    Task DeletePublishedAsync(DateTime olderThan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

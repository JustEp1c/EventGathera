using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Events.Infrastructure.Repositories.Implementations;

public class ProcessedMessageRepository : IProcessedMessageRepository
{
    private readonly EventsDbContext _context;

    public ProcessedMessageRepository(EventsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string messageId, string messageType, CancellationToken ct = default)
    {
        return await _context.ProcessedMessages
            .AnyAsync(pm => pm.MessageId == messageId && pm.MessageType == messageType, ct);
    }

    public async Task AddAsync(ProcessedMessage message, CancellationToken ct = default)
    {
        await _context.ProcessedMessages.AddAsync(message, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}

using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Domain.Entities;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Bookings.Infrastructure.Repositories.Implementations;

public class OutboxRepository : IOutboxRepository
{
    private readonly BookingsDbContext _context;

    public OutboxRepository(BookingsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default)
    {
        await _context.OutboxMessages.AddAsync(message, ct);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 100, CancellationToken ct = default)
    {
        return await _context.OutboxMessages
            .Where(o => o.Status == OutboxStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }

    public async Task MarkAsPublishedAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync([id], ct);
        if (message != null)
        {
            message.MarkAsPublished();
        }
    }

    public async Task MarkAsFailedAsync(Guid id, string error, CancellationToken ct = default)
    {
        var message = await _context.OutboxMessages.FindAsync([id], ct);
        if (message != null)
        {
            message.MarkAsFailed(error);
        }
    }

    public async Task DeletePublishedAsync(DateTime olderThan, CancellationToken ct = default)
    {
        await _context.OutboxMessages
            .Where(o => o.Status == OutboxStatus.Published && o.PublishedAt < olderThan)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}

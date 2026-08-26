using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Events.Infrastructure.Repositories.Implementations;


/// <inheritdoc/>
public class EventRepository : IEventRepository
{
    private readonly EventsDbContext _appDbContext;

    public EventRepository(EventsDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task AddEventAsync(Event @event, CancellationToken ct = default)
    {
        await _appDbContext.AddAsync(@event, ct);
    }

    public IQueryable<Event> GetAllEventsQuery()
    {
        return _appDbContext.Events;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken: ct);
    }

    public async Task<List<Event>> GetTopEventsAsync(int count, CancellationToken ct = default)
    {
        return await _appDbContext.Events
            .Where(e => e.TotalSeats > 0)
            .OrderByDescending(e => (double)(e.TotalSeats - e.AvailableSeats) / e.TotalSeats)
            .Take(count)
            .ToListAsync(ct);
    }

    public void RemoveEvent(Event @event, CancellationToken ct = default)
    {
        _appDbContext.Events.Remove(@event);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }
}

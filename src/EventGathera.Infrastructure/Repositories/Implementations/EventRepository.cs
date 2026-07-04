using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Domain;
using EventGathera.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Infrastructure.Repositories.Implementations;


/// <inheritdoc/>
public class EventRepository : IEventRepository
{
    private readonly AppDbContext _appDbContext;

    public EventRepository(AppDbContext appDbContext)
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

    public void RemoveEvent(Event @event, CancellationToken ct = default)
    {
        _appDbContext.Events.Remove(@event);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _appDbContext.SaveChangesAsync(ct);
    }
}

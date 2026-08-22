using EventGathera.Events.Application.Cache;
using EventGathera.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventGathera.Events.Infrastructure.Cache;

public class CacheService : ICacheService
{
    private readonly IDatabase _db;

    private readonly ILogger<CacheService> _logger;

    public CacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<CacheService> logger)
    {
        _db = connectionMultiplexer.GetDatabase();
        _logger = logger;
    }

    public async Task<Event?> GetEventByIdAsync(Guid id)
    {
        try
        {
            var key = $"event:{id}";

            var cached = await _db.StringGetAsync(key);

            if (!cached.HasValue)
            {
                return null;
            }

            return JsonSerializer.Deserialize<Event>(cached.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен при GET для Event {EventId}", id);
            return null;
        }
    }

    public async Task RemoveEventByIdAsync(Guid id)
    {
        try
        {
            var key = $"event:{id}";

            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен при REMOVE для Event {EventId}", id);
        }
    }

    public async Task SetEventAsync(Event @event, int ttl)
    {
        try
        {
            var json = JsonSerializer.Serialize(@event);

            var key = $"event:{@event.Id}";

            await _db.StringSetAsync(key, json, TimeSpan.FromMinutes(ttl));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis недоступен при SET для Event {EventId}", @event.Id);
        }
    }
}

using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.Services.Interfaces;

namespace EventGathera.Api.Services.Implementations;


/// <inheritdoc/>
public class EventService : IEventService
{
    private readonly EventStorage _storage;

    public EventService(EventStorage storage)
    {
        _storage = storage;
    }

    /// <inheritdoc/>
    public List<Event> GetAllEvents(string? title, DateTime? from, DateTime? to)
    {
        var events = _storage.Events;

        if (title != null && from != null && to != null)
        {
            events = events
                .Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase) &&
                    e.StartAt >= from &&
                    e.EndAt <= to)
                .ToList();
        }
        return events;
    }

    /// <inheritdoc/>
    public Event GetEventById(int id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new KeyNotFoundException($"Событие с ID {id} не найдено");
        }

        return foundEvent;
    }

    /// <inheritdoc/>
    public Event CreateEvent(EventRequest request)
    {
        var newEvent = new Event
        {
            Id = _storage.Events.Count == 0 ? 1 : _storage.Events.Max(e => e.Id) + 1,
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        _storage.Events.Add(newEvent);

        return newEvent;
    }

    /// <inheritdoc/>
    public void UpdateEvent(int id, EventRequest request)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new KeyNotFoundException($"Событие с ID {id} не найдено");
        }

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;
    }


    /// <inheritdoc/>
    public void DeleteEvent(int id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new KeyNotFoundException($"Событие с ID {id} не найдено");
        }

        _storage.Events.Remove(foundEvent);
    }

}

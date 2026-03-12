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
    public List<Event> GetAllEvents()
    {
        return _storage.Events;
    }

    /// <inheritdoc/>
    public Event? GetEventById(int id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        return foundEvent;
    }

    /// <inheritdoc/>
    public void CreateEvent(EventRequest request)
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
    }

    /// <inheritdoc/>
    public bool UpdateEvent(int id, EventRequest request)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            return false;
        }

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;

        return true;
    }


    /// <inheritdoc/>
    public bool DeleteEvent(int id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            return false;
        }

        _storage.Events.Remove(foundEvent);

        return true;
    }

}

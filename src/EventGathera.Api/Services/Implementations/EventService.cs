using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.Services.Interfaces;

namespace EventGathera.Api.Services.Implementations;


/// <inheritdoc/>
public class EventService : IEventService
{
    private List<Event> _events = [];

    /// <inheritdoc/>
    public List<Event> GetAllEvents()
    {
        return _events;
    }

    /// <inheritdoc/>
    public Event GetEventById(int id)
    {
        var foundEvent = _events.Find(e => e.Id == id)
            ?? throw new InvalidOperationException($"Событие с id = {id} не найдено");

        return foundEvent;
    }

    /// <inheritdoc/>
    public void CreateEvent(EventRequest request)
    {
        var newEvent = new Event
        {
            Id = _events.Count == 0 ? 1 : _events.Max(e => e.Id) + 1,
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        _events.Add(newEvent);
    }

    /// <inheritdoc/>
    public void UpdateEvent(int id, EventRequest request)
    {
        var foundEvent = _events.Find(e => e.Id == id)
            ?? throw new InvalidOperationException($"Событие с id = {id} не найдено");

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;
    }


    /// <inheritdoc/>
    public void DeleteEvent(int id)
    {
        var foundEvent = _events.Find(e => e.Id == id)
           ?? throw new InvalidOperationException($"Событие с id = {id} не найдено");

        _events.Remove(foundEvent);
    }

}

using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.DTO.Responses;
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
    public PaginatedResult<Event> GetAllEvents(EventQueryParams queryParams)
    {
        var events = _storage.Events;

        if (queryParams.Title != null && queryParams.From != null && queryParams.To != null)
        {
            events = events
                .Where(e => e.Title.Contains(queryParams.Title, StringComparison.OrdinalIgnoreCase) &&
                    e.StartAt >= queryParams.From &&
                    e.EndAt <= queryParams.To)
                .ToList();
        }

        var eventsOnPage = events
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToList();

        var paginatedResult = new PaginatedResult<Event>
        {
            TotalItems = events.Count,
            Items = eventsOnPage,
            CurrrentPage = queryParams.Page,
            ItemsOnCurrrentPage = eventsOnPage.Count
        };

        return paginatedResult;
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

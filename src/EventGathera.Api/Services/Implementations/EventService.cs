using EventGathera.Api.Contracts.DTO.Requests;
using EventGathera.Api.Contracts.DTO.Responses;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
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
        IEnumerable<Event> events = _storage.Events;

        if (!string.IsNullOrWhiteSpace(queryParams.Title))
        {
            events = events.Where(e => e.Title.Contains(queryParams.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (queryParams.From.HasValue)
        {
            events = events.Where(e => e.StartAt >= queryParams.From);
        }

        if (queryParams.To.HasValue)
        {
            events = events.Where(e => e.EndAt <= queryParams.To);
        }

        events = events.OrderBy(e => e.StartAt);

        var eventList = events.ToList();

        var eventsOnPage = eventList
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToList();

        var paginatedResult = new PaginatedResult<Event>
        {
            TotalItems = eventList.Count,
            Items = eventsOnPage,
            CurrrentPage = queryParams.Page,
            ItemsOnCurrrentPage = eventsOnPage.Count
        };

        return paginatedResult;
    }

    /// <inheritdoc/>
    public Event GetEventById(Guid id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        return foundEvent;
    }

    /// <inheritdoc/>
    public Event CreateEvent(EventRequest request)
    {
        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        _storage.Events.Add(newEvent);

        return newEvent;
    }

    /// <inheritdoc/>
    public void UpdateEvent(Guid id, EventRequest request)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;
    }


    /// <inheritdoc/>
    public void DeleteEvent(Guid id)
    {
        var foundEvent = _storage.Events.Find(e => e.Id == id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        _storage.Events.Remove(foundEvent);
    }

}

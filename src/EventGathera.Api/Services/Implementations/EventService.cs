using EventGathera.Api.Contracts.DTO.Requests;
using EventGathera.Api.Contracts.DTO.Responses;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Repositories.Interfaces;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Api.Services.Implementations;


/// <inheritdoc/>
public class EventService : IEventService
{
    private readonly IEventRepository _eventrepository;

    public EventService(IEventRepository eventrepository)
    {
        _eventrepository = eventrepository;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<Event>> GetAllEventsAsync(EventQueryParams queryParams)
    {
        IQueryable<Event> events = _eventrepository.GetAllEventsQuery();

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

        var totalItems = await events.CountAsync();

        var eventsOnPage = await events
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        var paginatedResult = new PaginatedResult<Event>
        {
            TotalItems = totalItems,
            Items = eventsOnPage,
            CurrrentPage = queryParams.Page,
            ItemsOnCurrrentPage = eventsOnPage.Count
        };

        return paginatedResult;
    }

    /// <inheritdoc/>
    public async Task<Event> GetEventByIdAsync(Guid id)
    {
        var foundEvent = await _eventrepository.GetEventByIdAsync(id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        return foundEvent;
    }

    /// <inheritdoc/>
    public async Task<Event> CreateEventAsync(EventRequest request)
    {
        var newEvent = new Event(
            request.Title,
            request.StartAt,
            request.EndAt,
            request.TotalSeats,
            request.Description
        );

        await _eventrepository.AddEventAsync(newEvent);

        await _eventrepository.SaveChangesAsync();

        return newEvent;
    }

    /// <inheritdoc/>
    public async Task UpdateEventAsync(Guid id, EventRequest request)
    {
        var foundEvent = await _eventrepository.GetEventByIdAsync(id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;

        await _eventrepository.SaveChangesAsync();
    }


    /// <inheritdoc/>
    public async Task DeleteEventAsync(Guid id)
    {
        var foundEvent = await _eventrepository.GetEventByIdAsync(id);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        _eventrepository.RemoveEvent(foundEvent);

        await _eventrepository.SaveChangesAsync();
    }

}

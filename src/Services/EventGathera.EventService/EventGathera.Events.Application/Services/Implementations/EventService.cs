using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.DTO.Requests;
using EventGathera.Events.Application.DTO.Responses;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Events.Application.Services.Implementations;


/// <inheritdoc/>
public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;

    private readonly ICacheService _cacheService;

    private const int EventTTL = 5;

    private const int TopEventsTTL = 10;

    private const int TopCount = 10;

    public EventService(IEventRepository eventRepository, ICacheService cacheService)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<Event>> GetAllEventsAsync(EventQueryParams queryParams, CancellationToken ct)
    {
        IQueryable<Event> events = _eventRepository.GetAllEventsQuery();

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
            .ToListAsync(ct);

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
    public async Task<Event> GetEventByIdAsync(Guid id, CancellationToken ct)
    {
        var cache = await _cacheService.GetEventByIdAsync(id);

        if (cache is not null)
        {
            return cache;
        }

        var foundEvent = await _eventRepository.GetEventByIdAsync(id, ct);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        await _cacheService.SetEventAsync(foundEvent, EventTTL);

        return foundEvent;
    }

    /// <inheritdoc/>
    public async Task<Event> CreateEventAsync(EventRequest request, CancellationToken ct)
    {
        var newEvent = new Event(
            request.Title,
            request.StartAt,
            request.EndAt,
            request.TotalSeats,
            request.Description
        );

        await _eventRepository.AddEventAsync(newEvent, ct);

        await _eventRepository.SaveChangesAsync(ct);

        await _cacheService.SetEventAsync(newEvent, EventTTL);

        return newEvent;
    }

    /// <inheritdoc/>
    public async Task UpdateEventAsync(Guid id, EventRequest request, CancellationToken ct)
    {
        var foundEvent = await _eventRepository.GetEventByIdAsync(id, ct);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        foundEvent.Title = request.Title;
        foundEvent.Description = request.Description;
        foundEvent.StartAt = request.StartAt;
        foundEvent.EndAt = request.EndAt;

        await _eventRepository.SaveChangesAsync(ct);

        await _cacheService.RemoveEventByIdAsync(id);
    }


    /// <inheritdoc/>
    public async Task DeleteEventAsync(Guid id, CancellationToken ct)
    {
        var foundEvent = await _eventRepository.GetEventByIdAsync(id, ct);

        if (foundEvent is null)
        {
            throw new ResourceNotFoundException($"Событие с ID {id} не найдено", id);
        }

        _eventRepository.RemoveEvent(foundEvent, ct);

        await _eventRepository.SaveChangesAsync(ct);

        await _cacheService.RemoveEventByIdAsync(id);
    }

    public async Task<List<Event>> GetTopEventsAsync(CancellationToken ct)
    {
        var cachedTop = await _cacheService.GetTopEvents(TopCount);

        if (cachedTop is not null)
        {
            return cachedTop;
        }

        var top = await _eventRepository.GetTopEventsAsync(TopCount, ct);

        await _cacheService.SetTopEvents(top, TopCount, TopEventsTTL);

        return top;
    }
}

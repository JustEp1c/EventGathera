using EventGathera.Events.Application.DTO.Requests;
using EventGathera.Events.Application.DTO.Responses;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventGathera.Presentation.Controllers
{
    [Route("events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService) 
        {
            _eventService = eventService
                ?? throw new ArgumentNullException(nameof(eventService));
        }

        /// <summary>
        /// Получить пагинированный список всех всех событий
        /// </summary>
        /// <param name="queryParams">Параметры запроса для событий</param>
        /// <returns>200, пагинированный список всех событий</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<Event>>> GetAllEvents([FromQuery] EventQueryParams queryParams)
        {
            var events = await _eventService.GetAllEventsAsync(queryParams);

            return events;
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <returns>200, если событие найдено</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEventById(Guid id)
        {
            var foundEvent = await _eventService.GetEventByIdAsync(id);

            return foundEvent;
        }

        /// <summary>
        /// Создать событие
        /// </summary>
        /// <param name="request">DTO нового события</param>
        /// <returns>201, если событие создалось</returns>
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequest request)
        {
            var result = await _eventService.CreateEventAsync(request);

            return CreatedAtAction(nameof(GetEventById), new { result.Id }, result);
        }

        /// <summary>
        /// Обновить событие
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <param name="request">DTO обновленного события</param>
        /// <returns>204, если событие обновлено</returns>
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] EventRequest request)
        {
            await _eventService.UpdateEventAsync(id, request);

            return NoContent();
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <param name="ct"></param>
        /// <returns>204, если событие удалено</returns>
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
        {
            await _eventService.DeleteEventAsync(id, ct);

            return NoContent();
        }

        /// <summary>
        /// Получить топ-10 событий с наибольшим процентом проданных мест
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>200, топ-10 событий</returns>
        [HttpGet("top")]
        public async Task<ActionResult<List<Event>>> GetTopEvents(CancellationToken ct)
        {
            var top = await _eventService.GetTopEventsAsync(ct);

            return top;
        }
    }
}

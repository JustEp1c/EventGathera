using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.DTO.Responses;
using EventGathera.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventGathera.Api.Controllers
{
    [Route("api/events")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        public EventsController(IEventService eventService) 
        {
            _eventService = eventService
                ?? throw new ArgumentNullException(nameof(IEventService));
        }

        /// <summary>
        /// Получить пагинированный список всех всех событий
        /// </summary>
        /// <param name="queryParams">Параметры запроса для событий</param>
        /// <returns>200, пагинированный список всех событий</returns>
        [HttpGet]
        public ActionResult<PaginatedResult<Event>> GetAllEvents([FromQuery] EventQueryParams queryParams)
        {
            var events = _eventService.GetAllEvents(queryParams);

            return events;
        }

        /// <summary>
        /// Получить событие по id
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <returns>200, если событие найдено</returns>
        [HttpGet("{id}")]
        public ActionResult<Event> GetEventById(int id)
        {
            var foundEvent = _eventService.GetEventById(id);

            return foundEvent;
        }

        /// <summary>
        /// Создать событие
        /// </summary>
        /// <param name="request">DTO нового события</param>
        /// <returns>201, если событие создалось</returns>
        [HttpPost]
        public IActionResult CreateEvent([FromBody] EventRequest request)
        {
            var result = _eventService.CreateEvent(request);

            return CreatedAtAction(nameof(GetEventById), new { result.Id }, result);
        }

        /// <summary>
        /// Обновить событие
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <param name="request">DTO обновленного события</param>
        /// <returns>204, если событие обновлено</returns>
        [HttpPut("{id}")]
        public IActionResult UpdateEvent(int id, [FromBody] EventRequest request)
        {
            _eventService.UpdateEvent(id, request);

            return NoContent();
        }

        /// <summary>
        /// Удалить событие
        /// </summary>
        /// <param name="id">Уникальный идентификатор</param>
        /// <returns>204, если событие удалено</returns>
        [HttpDelete("{id}")]
        public IActionResult DeleteEvent(int id)
        {
            _eventService.DeleteEvent(id);

            return NoContent();
        }
    }
}

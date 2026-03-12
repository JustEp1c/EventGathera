using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
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
        /// Получить список всех событий
        /// </summary>
        /// <returns>200, список всех событий</returns>
        [HttpGet]
        public ActionResult<List<Event>> GetAllEvents()
        {
            var events = _eventService.GetAllEvents();

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

            if (foundEvent == null)
            {
                return NotFound(foundEvent);
            }

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
            _eventService.CreateEvent(request);

            return CreatedAtAction(nameof(CreateEvent), new { request.Title, request.Description }, request);
        }
    }
}

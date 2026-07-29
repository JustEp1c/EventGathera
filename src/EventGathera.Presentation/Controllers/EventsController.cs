using EventGathera.Application.DTO.Requests;
using EventGathera.Application.DTO.Responses;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
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

        private readonly IBookingService _bookingService;

        public EventsController(IEventService eventService, IBookingService bookingService) 
        {
            _eventService = eventService
                ?? throw new ArgumentNullException(nameof(IEventService));
            _bookingService = bookingService 
                ?? throw new ArgumentNullException(nameof(IBookingService));
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        /// <returns>204, если событие удалено</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            await _eventService.DeleteEventAsync(id);

            return NoContent();
        }

        /// <summary>
        /// Создать бронь
        /// </summary>
        /// <param name="id">Уникальный идентификатор события</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>202, если бронь создана и отправлена на обработку</returns>
        [Authorize]
        [HttpPost("{id}/book")]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateBooking(Guid id, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не найден ID пользователя в токене");
            }

            var result = await _bookingService.CreateBookingAsync(id, userId, ct);

            return AcceptedAtAction(nameof(BookingsController.GetBookingById), 
                "Bookings", 
                new { bookingId = result.Id },
                new { result.Id, result.EventId, result.Status });
        }
    }
}

using EventGathera.Presentation.Domain;
using EventGathera.Presentation.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventGathera.Presentation.Controllers
{
    [Route("bookings")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService
                ?? throw new ArgumentNullException(nameof(IBookingService));
        }

        /// <summary>
        /// Получить бронь по ID
        /// </summary>
        /// <param name="bookingId">Уникальный идентификатор брони</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>200, если бронь найдена</returns>
        [HttpGet("{bookingId}")]
        public async Task<ActionResult<Booking>> GetBookingById(Guid bookingId, CancellationToken ct)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId, ct);

            return result;
        }
    }
}

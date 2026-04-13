using EventGathera.Api.Domain;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventGathera.Api.Controllers
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
        /// <returns>200, если бронь найдена</returns>
        [HttpGet("{bookingId}")]
        public async Task<ActionResult<Booking>> GetBookingById(Guid bookingId)
        {
            var result = await _bookingService.GetBookingByIdAsync(bookingId);

            return result;
        }
    }
}

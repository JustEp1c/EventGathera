using EventGathera.Application.Services.Interfaces;
using EventGathera.Domain;
using EventGathera.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                ?? throw new ArgumentNullException(nameof(bookingService));
        }

        /// <summary>
        /// Получить бронь по ID
        /// </summary>
        /// <param name="bookingId">Уникальный идентификатор брони</param>
        /// <param name="ct">Токен отмены</param>
        /// <returns>200, если бронь найдена</returns>
        [Authorize]
        [HttpGet("{bookingId}")]
        public async Task<ActionResult<Booking>> GetBookingById(Guid bookingId, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = User.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не найден ID пользователя в токене");
            }

            if (userRoleClaim == null || string.IsNullOrEmpty(userRoleClaim.Value))
            {
                return Unauthorized("Не найдена роль пользователя в токене");
            }

            if (!Enum.TryParse<Roles>(userRoleClaim.Value, false, out var userRole))
            {
                return Unauthorized($"Некорректная роль пользователя: {userRoleClaim.Value}");
            }

            var result = await _bookingService.GetBookingByIdAsync(bookingId, userId, userRole, ct);

            return result;
        }

        /// <summary>
        /// Отменить бронь
        /// </summary>
        /// <param name="bookingId">Id брони</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPut("cancel/{bookingId}")]
        public async Task<IActionResult> CancelBookingById(Guid bookingId, CancellationToken ct)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userRoleClaim = User.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Не найден ID пользователя в токене");
            }

            if (userRoleClaim == null || string.IsNullOrEmpty(userRoleClaim.Value))
            {
                return Unauthorized("Не найдена роль пользователя в токене");
            }

            if (!Enum.TryParse<Roles>(userRoleClaim.Value, false, out var userRole))
            {
                return Unauthorized($"Некорректная роль пользователя: {userRoleClaim.Value}");
            }

            await _bookingService.CancelBookingAsync(bookingId, userId, userRole, ct);

            return NoContent();
        }
    }
}

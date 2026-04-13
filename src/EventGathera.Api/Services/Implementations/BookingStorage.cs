using EventGathera.Api.Domain;

namespace EventGathera.Api.Services.Implementations;

/// <summary>
/// Хранилище бронирований в памяти
/// </summary>
public class BookingStorage
{
    /// <summary>
    /// Список бронирований, хранящийся в памяти
    /// </summary>
    public List<Booking> Bookings { get; } = [];
}

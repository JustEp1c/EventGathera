namespace EventGathera.Bookings.Domain.Exceptions;

/// <summary>
/// Превышение лимита активных бронирований у пользователя
/// </summary>
public class ExceedingActiveBookingLimitException : Exception
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public Guid UserId { get; }
    public ExceedingActiveBookingLimitException(string message) : base(message) { }

    public ExceedingActiveBookingLimitException(string message, Guid id) : base(message)
    {
        UserId = id;
    }
}

namespace EventGathera.Domain.Exceptions;

/// <summary>
/// Исключение бронирования прошедшего события
/// </summary>
public class ExpiredEventBookingException : Exception
{
    /// <summary>
    /// ID события
    /// </summary>
    public Guid EventId { get; }
    public ExpiredEventBookingException(string message) : base(message) { }

    public ExpiredEventBookingException(string message, Guid id) : base(message)
    {
        EventId = id;
    }
}

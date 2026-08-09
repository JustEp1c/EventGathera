namespace EventGathera.Bookings.Domain.Exceptions;

/// <summary>
/// Отсутствие прав на операцию
/// </summary>
public class ForbiddenOperationException : Exception
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public Guid UserId { get; }
    public ForbiddenOperationException(string message) : base(message) { }

    public ForbiddenOperationException(string message, Guid id) : base(message)
    {
        UserId = id;
    }
}

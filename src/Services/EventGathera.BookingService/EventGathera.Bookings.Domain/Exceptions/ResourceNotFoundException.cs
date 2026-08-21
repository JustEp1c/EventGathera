namespace EventGathera.Bookings.Domain.Exceptions;

/// <summary>
/// Тип исключения для ненайденного ресурса
/// </summary>
public class ResourceNotFoundException : Exception
{
    /// <summary>
    /// ID ненайденного ресурса
    /// </summary>
    public Guid ResourceId { get; }

    public ResourceNotFoundException(string message) : base(message) { }

    public ResourceNotFoundException(string message, Guid id) : base(message) 
    {
        ResourceId = id;
    }
}

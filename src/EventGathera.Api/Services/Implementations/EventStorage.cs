using EventGathera.Api.Domain;

namespace EventGathera.Api.Services.Implementations;

/// <summary>
/// Хранилище событий в памяти
/// </summary>
public class EventStorage
{
    /// <summary>
    /// Список событий, хранящийся в памяти
    /// </summary>
    public List<Event> Events { get; } = [];
}

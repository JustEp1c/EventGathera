using EventGathera.Shared.Contracts;

namespace EventGathera.Events.Application.Kafka;

public interface IEventPublisher
{
    Task PublishEventSeatUnavailableAsync(EventSeatUnavailable @event, CancellationToken ct = default);
    Task PublishEventSeatReservedAsync(EventSeatReserved @event, CancellationToken ct = default);
}

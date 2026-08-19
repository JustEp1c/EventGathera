using EventGathera.Shared.Contracts;

namespace EventGathera.Bookings.Application.Kafka;


public interface IEventPublisher
{
    Task PublishBookingCreatedAsync(BookingCreated @event, CancellationToken ct = default);
    Task PublishBookingConfirmedAsync(BookingConfirmed @event, CancellationToken ct = default);
    Task PublishBookingRejectedAsync(BookingRejected @event, CancellationToken ct = default);
}

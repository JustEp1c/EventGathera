namespace EventGathera.Shared.Topics;

public static class KafkaTopics
{
    public const string BookingCreatedTopic = "booking-created";
    public const string EventSeatReservedTopic = "event-seat-reserved";
    public const string EventSeatUnavailableTopic = "event-seat-unavailable";
    public const string BookingConfirmedTopic = "booking-confirmed";
    public const string BookingRejectedTopic = "booking-rejected";
}

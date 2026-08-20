using Confluent.Kafka;
using EventGathera.Bookings.Application.Kafka;
using EventGathera.Shared.Contracts;
using EventGathera.Shared.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace EventGathera.Bookings.Infrastructure.Kafka;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    private readonly ILogger<KafkaEventPublisher> _logger;

    private bool _disposed = false;

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmed @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.BookingConfirmedTopic, @event.EventId.ToString(), @event, ct);
    }

    public async Task PublishBookingCreatedAsync(BookingCreated @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.BookingCreatedTopic, @event.EventId.ToString(), @event, ct);
    }

    public async Task PublishBookingRejectedAsync(BookingRejected @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.BookingRejectedTopic, @event.EventId.ToString(), @event, ct);
    }

    public async Task PublishBookingCancelledAsync(BookingCancelled @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.BookingCancelledTopic, @event.EventId.ToString(), @event, ct);
    }

    private async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = json
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);

            _logger.LogInformation(
                    "Опубликовано событие {EventType} в {Topic}, Partition: {Partition}, Offset: {Offset}",
                    typeof(T).Name,
                    topic,
                    result.Partition.Value,
                    result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Ошибка публикации события {EventType} в {Topic}: {Error}",
                typeof(T).Name,
                topic,
                ex.Error.Reason);

            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
            _disposed = true;
        }
    }
}

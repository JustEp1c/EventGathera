using Confluent.Kafka;
using EventGathera.Events.Application.Kafka;
using EventGathera.Shared.Contracts;
using EventGathera.Shared.Topics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventGathera.Events.Infrastructure.Kafka;

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

    public async Task PublishEventSeatUnavailableAsync(EventSeatUnavailable @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.EventSeatUnavailableTopic, @event.EventId.ToString(), @event, ct);
    }

    public async Task PublishEventSeatReservedAsync(EventSeatReserved @event, CancellationToken ct = default)
    {
        await PublishAsync(KafkaTopics.EventSeatReservedTopic, @event.EventId.ToString(), @event, ct);
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

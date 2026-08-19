using Confluent.Kafka;
using EventGathera.Events.Application.Kafka;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Shared.Contracts;
using EventGathera.Shared.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventGathera.Events.Infrastructure.Kafka;

public class KafkaEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafkaEventConsumer> _logger;
    private readonly IEventPublisher _eventPublisher;

    public KafkaEventConsumer(IConsumer<string, string> consumer, IServiceScopeFactory serviceScopeFactory, ILogger<KafkaEventConsumer> logger, IEventPublisher eventPublisher)
    {
        _consumer = consumer;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(KafkaTopics.BookingCreatedTopic);

        _logger.LogInformation("Подписан на топик: {Topic}", KafkaTopics.BookingCreatedTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);

                    if (result?.Message == null)
                        continue;

                    _logger.LogInformation(
                        "Получено сообщение из топика {Topic}, Key: {Key}, Partition: {Partition}, Offset: {Offset}",
                        result.Topic,
                        result.Message.Key,
                        result.Partition.Value,
                        result.Offset.Value);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                    var bookingCreatedMessage = JsonSerializer.Deserialize<BookingCreated>(result.Message.Value);

                    await ProcessMessageAsync(result.Message, eventRepository, stoppingToken);

                    _consumer.Commit(result);

                    _logger.LogInformation("Сообщение обработано и закоммичено");
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка при потреблении сообщения: {Error}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки сообщения");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Потребление остановлено");
        }
        finally 
        {
            _consumer.Close();
            _logger.LogInformation("Консьюмер закрыт");
        }
    }

    private async Task ProcessMessageAsync(Message<string, string> message, IEventRepository eventRepository, CancellationToken stoppingToken)
    {
        try
        {
            var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(message.Value);

            if (bookingCreated == null)
            {
                _logger.LogWarning("Не удалось десериализовать сообщение");
                return;
            }

            var foundEvent = await eventRepository.GetEventByIdAsync(bookingCreated.EventId, stoppingToken);

            if (foundEvent == null)
            {
                _logger.LogWarning(
                    "Событие {EventId} не найдено для брони {BookingId}",
                    bookingCreated.EventId,
                    bookingCreated.BookingId);

                await _eventPublisher.PublishEventSeatUnavailableAsync(
                    new EventSeatUnavailable
                    {
                        BookingId = bookingCreated.BookingId,
                        EventId = bookingCreated.EventId,
                        UserId = bookingCreated.UserId,
                        Reason = $"Событие с ID {bookingCreated.EventId} не найдено",
                        FailedAt = DateTime.UtcNow
                    },
                    stoppingToken);

                return;
            }

            if (foundEvent.StartAt <= bookingCreated.CreatedAt)
            {
                _logger.LogWarning(
                    "Событие {EventId} уже началось. Бронь {BookingId} отклонена",
                    bookingCreated.EventId,
                    bookingCreated.BookingId);

                await _eventPublisher.PublishEventSeatUnavailableAsync(
                    new EventSeatUnavailable
                    {
                        BookingId = bookingCreated.BookingId,
                        EventId = bookingCreated.EventId,
                        UserId = bookingCreated.UserId,
                        Reason = $"Событие '{foundEvent.Title}' уже началось",
                        FailedAt = DateTime.UtcNow
                    },
                    stoppingToken);

                return;
            }

            if (!foundEvent.TryReserveSeats())
            {
                _logger.LogWarning(
                        "Нет свободных мест на событие {EventId}. Бронь {BookingId} отклонена",
                        bookingCreated.EventId,
                        bookingCreated.BookingId);

                await _eventPublisher.PublishEventSeatUnavailableAsync(
                    new EventSeatUnavailable
                    {
                        BookingId = bookingCreated.BookingId,
                        EventId = bookingCreated.EventId,
                        UserId = bookingCreated.UserId,
                        Reason = $"Нет свободных мест на событие '{foundEvent.Title}'",
                        FailedAt = DateTime.UtcNow
                    },
                    stoppingToken);

                return;
            }

            await eventRepository.SaveChangesAsync(stoppingToken);

            await _eventPublisher.PublishEventSeatReservedAsync(
                    new EventSeatReserved
                    {
                        BookingId = bookingCreated.BookingId,
                        EventId = bookingCreated.EventId,
                        UserId = bookingCreated.UserId,
                        SeatsReserved = 1,
                        AvailableSeats = foundEvent.AvailableSeats,
                        ReservedAt = DateTime.UtcNow
                    },
                    stoppingToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации JSON");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки сообщения: {Message}", ex.Message);
            throw;
        }
    }
}

using Confluent.Kafka;
using EventGathera.Events.Application.Kafka;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Shared.Contracts;
using EventGathera.Shared.Topics;
using Microsoft.EntityFrameworkCore;
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
        _consumer.Subscribe(
        [
            KafkaTopics.BookingCreatedTopic,
            KafkaTopics.BookingCancelledTopic
        ]);

        _logger.LogInformation("Подписан на топики: {Topics}", string.Join(", ", KafkaTopics.BookingCreatedTopic, KafkaTopics.BookingCancelledTopic));

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
                    var processedMessageRepo = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();

                    if (result.Topic == KafkaTopics.BookingCreatedTopic)
                    {
                        await ProcessBookingCreatedAsync(result.Message, eventRepository, processedMessageRepo, stoppingToken);
                    }
                    else if (result.Topic == KafkaTopics.BookingCancelledTopic)
                    {
                        await ProcessBookingCancelledAsync(result.Message, eventRepository, processedMessageRepo, stoppingToken);
                    }

                    _consumer.StoreOffset(result);
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

    private async Task ProcessBookingCancelledAsync(Message<string, string> message, IEventRepository eventRepository, IProcessedMessageRepository processedMessageRepository, CancellationToken stoppingToken)
    {
        try
        {
            var bookingCancelled = JsonSerializer.Deserialize<BookingCancelled>(message.Value);

            if (bookingCancelled == null)
            {
                _logger.LogWarning("Не удалось десериализовать BookingCancelled");
                return;
            }

            var messageId = $"{bookingCancelled.BookingId}_{bookingCancelled.EventId}";

            if (await processedMessageRepository.ExistsAsync(messageId, "BookingCancelled", stoppingToken))
            {
                _logger.LogWarning(
                    "Сообщение об отмене {MessageId} уже обработано. Пропускаем.",
                    messageId);
                return;
            }

            var foundEvent = await eventRepository.GetEventByIdAsync(bookingCancelled.EventId, stoppingToken);

            if (foundEvent == null)
            {
                _logger.LogWarning(
                    "Событие {EventId} не найдено для отмены брони {BookingId}",
                    bookingCancelled.EventId,
                    bookingCancelled.BookingId);
                return;
            }

            foundEvent.ReleaseSeats();

            var processedMessage = new ProcessedMessage(messageId, "BookingCancelled");
            await processedMessageRepository.AddAsync(processedMessage, stoppingToken);

            await eventRepository.SaveChangesAsync(stoppingToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            _logger.LogWarning(
                "Сообщение об отмене уже обработано. Пропускаем.");
            return;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации BookingCancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки BookingCancelled: {Message}", ex.Message);
            throw;
        }
    }

    private async Task ProcessBookingCreatedAsync(Message<string, string> message, IEventRepository eventRepository, IProcessedMessageRepository processedMessageRepository, CancellationToken stoppingToken)
    {
        try
        {
            var bookingCreated = JsonSerializer.Deserialize<BookingCreated>(message.Value);

            if (bookingCreated == null)
            {
                _logger.LogWarning("Не удалось десериализовать BookingCreated");
                return;
            }

            var messageId = $"{bookingCreated.BookingId}_{bookingCreated.EventId}";

            if (await processedMessageRepository.ExistsAsync(messageId, "BookingCreated", stoppingToken))
            {
                _logger.LogWarning(
                    "Сообщение {MessageId} уже обработано. Пропускаем.",
                    messageId);
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

            var processedMessage = new ProcessedMessage(messageId, "BookingCreated");
            await processedMessageRepository.AddAsync(processedMessage, stoppingToken);

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
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            _logger.LogWarning("Сообщение уже обработано. Пропускаем.");
            return;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации BookingCreated");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки BookingCreated: {Message}", ex.Message);
            throw;
        }
    }
}

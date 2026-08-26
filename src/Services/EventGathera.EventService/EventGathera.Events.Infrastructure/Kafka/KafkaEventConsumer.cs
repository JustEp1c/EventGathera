using Confluent.Kafka;
using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.Repositories.Interfaces;
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

    public KafkaEventConsumer(IConsumer<string, string> consumer, IServiceScopeFactory serviceScopeFactory, ILogger<KafkaEventConsumer> logger)
    {
        _consumer = consumer;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
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
                    var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                    var processedMessageRepo = scope.ServiceProvider.GetRequiredService<IProcessedMessageRepository>();
                    var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                    if (result.Topic == KafkaTopics.BookingCreatedTopic)
                    {
                        await ProcessBookingCreatedAsync(result.Message, eventRepo, processedMessageRepo, outboxRepo, cacheService, stoppingToken);
                    }
                    else if (result.Topic == KafkaTopics.BookingCancelledTopic)
                    {
                        await ProcessBookingCancelledAsync(result.Message, eventRepo, processedMessageRepo, cacheService, stoppingToken);
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

    private async Task ProcessBookingCancelledAsync(Message<string, string> message, IEventRepository eventRepository, IProcessedMessageRepository processedMessageRepository, ICacheService cacheService, CancellationToken stoppingToken)
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

            await processedMessageRepository.SaveChangesAsync(stoppingToken);

            await cacheService.RemoveEventByIdAsync(foundEvent.Id);
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

    private async Task ProcessBookingCreatedAsync(Message<string, string> message, IEventRepository eventRepository, IProcessedMessageRepository processedMessageRepository, IOutboxRepository outboxRepository, ICacheService cacheService, CancellationToken stoppingToken)
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
                await PublishSeatUnavailableWithOutboxAsync(
                    bookingCreated,
                    $"Событие с ID {bookingCreated.EventId} не найдено",
                    outboxRepository,
                    processedMessageRepository,
                    messageId,
                    stoppingToken);

                return;
            }

            if (foundEvent.StartAt <= bookingCreated.CreatedAt)
            {
                await PublishSeatUnavailableWithOutboxAsync(
                    bookingCreated,
                    $"Событие '{foundEvent.Title}' уже началось",
                    outboxRepository,
                    processedMessageRepository,
                    messageId,
                    stoppingToken);

                return;
            }

            if (!foundEvent.TryReserveSeats())
            {
                await PublishSeatUnavailableWithOutboxAsync(
                    bookingCreated,
                    $"Нет свободных мест на событие '{foundEvent.Title}'",
                    outboxRepository,
                    processedMessageRepository,
                    messageId,
                    stoppingToken);

                return;
            }

            var processedMessage = new ProcessedMessage(messageId, "BookingCreated");
            await processedMessageRepository.AddAsync(processedMessage, stoppingToken);

            var seatReserved = new EventSeatReserved
            {
                BookingId = bookingCreated.BookingId,
                EventId = bookingCreated.EventId,
                UserId = bookingCreated.UserId,
                SeatsReserved = 1,
                AvailableSeats = foundEvent.AvailableSeats,
                ReservedAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage(
               "EventSeatReserved",
               JsonSerializer.Serialize(seatReserved)
           );
            await outboxRepository.AddAsync(outboxMessage, stoppingToken);

            await outboxRepository.SaveChangesAsync(stoppingToken);

            await cacheService.RemoveEventByIdAsync(foundEvent.Id);
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

    private async Task PublishSeatUnavailableWithOutboxAsync(BookingCreated bookingCreated, string reason, IOutboxRepository outboxRepository, IProcessedMessageRepository processedMessageRepository, string messageId, CancellationToken stoppingToken)
    {
        var processedMessage = new ProcessedMessage(messageId, "BookingCreated");
        await processedMessageRepository.AddAsync(processedMessage, stoppingToken);

        var seatUnavailable = new EventSeatUnavailable
        {
            BookingId = bookingCreated.BookingId,
            EventId = bookingCreated.EventId,
            UserId = bookingCreated.UserId,
            Reason = reason,
            FailedAt = DateTime.UtcNow
        };

        var outboxMessage = new OutboxMessage(
            "EventSeatUnavailable",
            JsonSerializer.Serialize(seatUnavailable)
        );
        await outboxRepository.AddAsync(outboxMessage, stoppingToken);

        await processedMessageRepository.SaveChangesAsync(stoppingToken);

        _logger.LogWarning("Место НЕ зарезервировано. Причина: {Reason}", reason);
    }
}

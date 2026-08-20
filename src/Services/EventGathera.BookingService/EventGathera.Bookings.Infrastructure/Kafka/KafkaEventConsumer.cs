using Confluent.Kafka;
using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Domain.Entities;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Shared.Contracts;
using EventGathera.Shared.Topics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventGathera.Bookings.Infrastructure.Kafka;

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
        _consumer.Subscribe([KafkaTopics.EventSeatReservedTopic, KafkaTopics.EventSeatUnavailableTopic]);

        _logger.LogInformation("Подписан на топики: {Topics}", string.Join(", ", KafkaTopics.EventSeatReservedTopic, KafkaTopics.EventSeatUnavailableTopic));
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
                    var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                    if (result.Topic == KafkaTopics.EventSeatReservedTopic)
                    {
                        await HandleSeatReservedAsync(result.Message.Value, bookingRepo, outboxRepository, stoppingToken);
                    }
                    else if (result.Topic == KafkaTopics.EventSeatUnavailableTopic)
                    {
                        await HandleSeatUnavailableAsync(result.Message.Value, bookingRepo, outboxRepository, stoppingToken);
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

    private async Task HandleSeatUnavailableAsync(string value, IBookingRepository bookingRepo, IOutboxRepository outboxRepo, CancellationToken stoppingToken)
    {
        try
        {
            var unavailable = JsonSerializer.Deserialize<EventSeatUnavailable>(value);

            if (unavailable == null)
            {
                _logger.LogWarning("Не удалось десериализовать EventSeatUnavailable");
                return;
            }

            var booking = await bookingRepo.GetBookingByIdAsync(unavailable.BookingId, stoppingToken);

            if (booking == null)
            {
                _logger.LogWarning("Бронь {BookingId} не найдена", unavailable.BookingId);
                return;
            }

            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogWarning(
                    "Бронь {BookingId} уже обработана со статусом {Status}",
                    booking.Id,
                    booking.Status);
                return;
            }

            booking.Reject();

            var bookingRejected = new BookingRejected
            {
                BookingId = unavailable.BookingId,
                EventId = unavailable.EventId,
                UserId = unavailable.UserId,
                Reason = unavailable.Reason,
                RejectedAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage(
                "BookingRejected",
                JsonSerializer.Serialize(bookingRejected)
            );

            await outboxRepo.AddAsync(outboxMessage, stoppingToken);

            await bookingRepo.SaveChangesAsync(stoppingToken);

            _logger.LogWarning(
                "Бронь {BookingId} отклонена. Причина: {Reason}",
                booking.Id,
                unavailable.Reason);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки EventSeatUnavailable");
            throw;
        }
    }

    private async Task HandleSeatReservedAsync(string value, IBookingRepository bookingRepo, IOutboxRepository outboxRepo, CancellationToken stoppingToken)
    {
        try
        {
            var reserved = JsonSerializer.Deserialize<EventSeatReserved>(value);

            if (reserved == null)
            {
                _logger.LogWarning("Не удалось десериализовать EventSeatReserved");
                return;
            }

            var booking = await bookingRepo.GetBookingByIdAsync(reserved.BookingId, stoppingToken);

            if (booking == null)
            {
                _logger.LogWarning("Бронь {BookingId} не найдена", reserved.BookingId);
                return;
            }

            if (booking.Status != BookingStatus.Pending)
            {
                _logger.LogWarning(
                    "Бронь {BookingId} уже обработана со статусом {Status}",
                    booking.Id,
                    booking.Status);
                return;
            }

            booking.Confirm();

            var bookingConfirmed = new BookingConfirmed
            {
                BookingId = reserved.BookingId,
                EventId = reserved.EventId,
                UserId = reserved.UserId,
                ConfirmedAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage(
                "BookingConfirmed",
                JsonSerializer.Serialize(bookingConfirmed)
            );

            await outboxRepo.AddAsync(outboxMessage, stoppingToken);


            await bookingRepo.SaveChangesAsync(stoppingToken);

        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ошибка десериализации");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка обработки EventSeatReserved");
            throw;
        }
    }
}

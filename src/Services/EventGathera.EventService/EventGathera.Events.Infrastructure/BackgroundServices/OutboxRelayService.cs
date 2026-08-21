using EventGathera.Events.Application.Kafka;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Shared.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventGathera.Events.Infrastructure.BackgroundServices;

/// <summary>
/// Фоновый сервис для публикации сообщений из Outbox
/// </summary>
public class OutboxRelayService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxRelayService> _logger;
    private readonly int _batchSize = 100;
    private readonly int _pollingIntervalMs = 5000;
    private readonly int _maxRetryCount = 3;

    public OutboxRelayService(IServiceScopeFactory serviceScopeFactory, ILogger<OutboxRelayService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxRelayService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                var pendingMessages = await outboxRepository.GetPendingMessagesAsync(_batchSize, stoppingToken);

                if (!pendingMessages.Any())
                {
                    await Task.Delay(_pollingIntervalMs, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Найдено {Count} сообщений в Outbox", pendingMessages.Count());

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await PublishMessageAsync(message, eventPublisher, stoppingToken);

                        message.MarkAsPublished();
                        await outboxRepository.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "Сообщение {MessageId} типа {Type} опубликовано в {PublishedAt}",
                            message.Id,
                            message.Type,
                            message.PublishedAt);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Ошибка публикации сообщения {MessageId} типа {Type}",
                            message.Id,
                            message.Type);

                        message.MarkAsFailed(ex.Message);
                        await outboxRepository.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в OutboxRelayService");
                await Task.Delay(_pollingIntervalMs, stoppingToken);
            }
        }

        _logger.LogInformation("OutboxRelayService остановлен");
    }

    private async Task PublishMessageAsync(
        OutboxMessage message,
        IEventPublisher eventPublisher,
        CancellationToken ct)
    {
        if (message.RetryCount >= _maxRetryCount)
        {
            _logger.LogWarning(
                "Сообщение {MessageId} превысило лимит попыток ({Max}). Пропускаем.",
                message.Id,
                _maxRetryCount);
            message.MarkAsFailed("Превышен лимит попыток");
            return;
        }

        switch (message.Type)
        {
            case "EventSeatReserved":
                var reserved = JsonSerializer.Deserialize<EventSeatReserved>(message.Payload);
                if (reserved != null)
                {
                    await eventPublisher.PublishEventSeatReservedAsync(reserved, ct);
                }
                break;

            case "EventSeatUnavailable":
                var unavailable = JsonSerializer.Deserialize<EventSeatUnavailable>(message.Payload);
                if (unavailable != null)
                {
                    await eventPublisher.PublishEventSeatUnavailableAsync(unavailable, ct);
                }
                break;

            default:
                _logger.LogWarning("Неизвестный тип сообщения: {Type}", message.Type);
                break;
        }

        message.IncrementRetry();
    }
}

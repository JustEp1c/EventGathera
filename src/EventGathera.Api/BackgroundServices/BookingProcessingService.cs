using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Api.BackgroundServices;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingProcessingService : BackgroundService
{
    private readonly BookingStorage _storage;
    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(BookingStorage storage, ILogger<BookingProcessingService> logger)
    {  
        _storage = storage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис для обработки бронирований запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var booking in _storage.Bookings)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (booking.Status == BookingStatus.Pending)
                {
                    _logger.LogInformation("Обработка брони {BookingId} начата", booking.Id);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                        booking.Status = BookingStatus.Confirmed;
                        booking.ProcessedAt = DateTime.UtcNow;

                        _logger.LogInformation("Обработка брони {BookingId} завершена, статус: {BookingStatus}", booking.Id, booking.Status);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Обработка брони {BookingId} отменена", booking.Id);

                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке брони");
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        _logger.LogInformation("Сервис для обработки бронирований остановлен");
    }
}

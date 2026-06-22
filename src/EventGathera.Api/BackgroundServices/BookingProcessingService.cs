using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Api.BackgroundServices;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingProcessingService : BackgroundService
{
    private readonly BookingStorage _bookingStorage;

    private readonly EventStorage _eventStorage;

    private readonly ILogger<BookingProcessingService> _logger;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    public BookingProcessingService(BookingStorage bookingStorage, EventStorage eventStorage, ILogger<BookingProcessingService> logger)
    {
        _bookingStorage = bookingStorage;
        _eventStorage = eventStorage;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис для обработки бронирований запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_bookingStorage.Bookings.Any(b => b.Status == BookingStatus.Pending))
            {
                var pendingBookings = _bookingStorage.Bookings
                    .Where(b => b.Status == BookingStatus.Pending)
                    .ToList();

                var tasks = pendingBookings.Select(booking =>
                    ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }

        _logger.LogInformation("Сервис для обработки бронирований остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Обработка брони {BookingId} начата", booking.Id);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var foundEvent = _eventStorage.Events.Find(e => e.Id == booking.EventId);

        await _processingSemaphore.WaitAsync();

        try
        {
            if (foundEvent is null)
            {
                booking.Status = BookingStatus.Rejected;
                _logger.LogWarning("Событие {EventId} больше не доступно", booking.EventId);
            }
            else
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;
            }
                
            _logger.LogInformation("Обработка брони {BookingId} завершена, статус: {BookingStatus}", booking.Id, booking.Status);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Обработка брони {BookingId} отменена", booking.Id);
        }
        catch (Exception ex)
        {
            booking.Status = BookingStatus.Rejected;
            if (foundEvent is not null)
            {
                foundEvent.ReleaseSeats();
            }
            _logger.LogError(ex, "Ошибка при обработке брони");
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}

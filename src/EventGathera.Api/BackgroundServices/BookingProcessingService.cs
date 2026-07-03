using EventGathera.Api.Constants;
using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Repositories.Interfaces;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Api.BackgroundServices;

/// <summary>
/// Фоновый сервис для обработки бронирований
/// </summary>
public class BookingProcessingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(IServiceScopeFactory serviceScopeFactory, ILogger<BookingProcessingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Сервис для обработки бронирований запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                    var pendingBookings = await bookingRepo.GetAllBookingsQuery()
                        .Where(b => b.Status == BookingStatus.Pending)
                        .Include(b => b.Event)
                        .ToListAsync(stoppingToken);

                    if (pendingBookings.Any())
                    {
                        _logger.LogInformation("Найдено {Count} бронирований для обработки", pendingBookings.Count);

                        var tasks = pendingBookings.Select(booking =>
                            ProcessBookingAsync(booking, stoppingToken));
                        await Task.WhenAll(tasks);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении бронирований из БД");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessingConstants.PollingIntervalInSeconds), stoppingToken);

        }

        _logger.LogInformation("Сервис для обработки бронирований остановлен");
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Обработка брони {BookingId} начата", booking.Id);

        await Task.Delay(TimeSpan.FromSeconds(ProcessingConstants.ProcessingDelayInSeconds), stoppingToken);

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            try
            {
                var currentBooking = await bookingRepo.GetBookingByIdAsync(booking.Id, stoppingToken);

                if (currentBooking is null)
                {
                    _logger.LogWarning("Бронь {BookingId} не найдена в БД", booking.Id);
                    return;
                }

                if (currentBooking.Status != BookingStatus.Pending)
                {
                    _logger.LogInformation("Бронь {BookingId} уже обработана со статусом {Status}",
                        currentBooking.Id, currentBooking.Status);
                    return;
                }

                var foundEvent = await eventRepo.GetEventByIdAsync(currentBooking.EventId, stoppingToken);

                if (foundEvent is null)
                {
                    currentBooking.Reject();
                    _logger.LogWarning("Событие {EventId} больше не доступно", currentBooking.EventId);
                }
                else
                {
                    if (foundEvent.AvailableSeats > 0)
                    {
                        currentBooking.Confirm();
                        _logger.LogInformation("Бронь {BookingId} подтверждена", currentBooking.Id);
                    }
                    else
                    {
                        currentBooking.Reject();
                        _logger.LogWarning("Бронь {BookingId} отклонена - нет свободных мест", currentBooking.Id);
                    }
                }

                await bookingRepo.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Обработка брони {BookingId} завершена, статус: {BookingStatus}", booking.Id, booking.Status);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Конфликт при обновлении брони {BookingId}", booking.Id);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Обработка брони {BookingId} отменена", booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке брони {BookingId}", booking.Id);

                try
                {
                    var currentBooking = await bookingRepo.GetBookingByIdAsync(booking.Id, stoppingToken);

                    if (currentBooking != null && currentBooking.Status == BookingStatus.Pending)
                    {
                        currentBooking.Reject();
                        await bookingRepo.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Не удалось отклонить бронь {BookingId}", booking.Id);
                }
            }
        }
    }
}

using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Application.Services.Implementations;
using EventGathera.Bookings.Application.Services.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Entities.Domain;
using EventGathera.Bookings.Infrastructure.DataAccess;
using EventGathera.Bookings.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Tests;

public class BookingStatusAndSeatsTests : IDisposable
{
    private readonly BookingsDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BookingStatusAndSeatsTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<BookingsDbContext>();
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(_eventId, _testUserId);

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
        Assert.True(booking.ProcessedAt >= DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Reject_ShouldChangeStatusToRejectedAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(_eventId, _testUserId);

        // Act
        booking.Reject();

        // Assert
        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelledAndSetProcessedAt()
    {
        // Arrange
        var booking = new Booking(_eventId, _testUserId);

        // Act
        booking.Cancel();

        // Assert
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.True(booking.ProcessedAt <= DateTime.UtcNow);
        Assert.True(booking.ProcessedAt >= DateTime.UtcNow.AddSeconds(-5));
    }

    public void Dispose()
    {
        // Очищаем InMemory БД после каждого теста
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
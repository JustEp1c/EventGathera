using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Application.Services.Implementations;
using EventGathera.Bookings.Application.Services.Interfaces;
using EventGathera.Bookings.Domain.Enums;
using EventGathera.Bookings.Domain.Exceptions;
using EventGathera.Bookings.Entities.Domain;
using EventGathera.Bookings.Infrastructure.DataAccess;
using EventGathera.Bookings.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace EventGathera.Tests;

public class BookingServiceGetBookingByIdTests : IDisposable
{
    private readonly IBookingService _bookingService;
    private readonly BookingsDbContext _dbContext;
    private readonly Guid _existingBookingId;
    private readonly Guid _existingEventId;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public BookingServiceGetBookingByIdTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<BookingsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<BookingsDbContext>();
        _bookingService = _serviceProvider.GetRequiredService<IBookingService>();

        _existingEventId = Guid.NewGuid();
        _existingBookingId = Guid.NewGuid();

        var testBooking = new Booking(
            _existingEventId,
            _testUserId
        )
        {
            Id = _existingBookingId
        };

        _dbContext.Bookings.Add(testBooking);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetBookingById_WithValidBookingId_ShouldReturnFoundBooking()
    {
        // Arrange
        var ct = CancellationToken.None;

        // Act
        var result = await _bookingService.GetBookingByIdAsync(_existingBookingId, _testUserId, Roles.User, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_existingEventId, result.EventId);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetBookingById_WithNonExistingBookingId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        var ct = CancellationToken.None;
        Guid nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _bookingService.GetBookingByIdAsync(nonExistingId, _testUserId, Roles.User, ct));

        Assert.Equal($"Бронь с ID {nonExistingId} не найдена", exception.Message);
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

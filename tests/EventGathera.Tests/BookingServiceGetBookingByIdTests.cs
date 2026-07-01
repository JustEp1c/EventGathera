using EventGathera.Api.BackgroundServices;
using EventGathera.Api.Contracts.Enums;
using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventGathera.Tests;

public class BookingServiceGetBookingByIdTests
{
    private readonly IBookingService _bookingService;
    private readonly AppDbContext _dbContext;
    private readonly Guid _existingBookingId;
    private readonly Guid _existingEventId;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;

    public BookingServiceGetBookingByIdTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _bookingService = _serviceProvider.GetRequiredService<IBookingService>();

        _existingEventId = Guid.NewGuid();

        var testEvent = new Event(
            title: "Test Event",
            startAt: DateTime.UtcNow.AddDays(1),
            endAt: DateTime.UtcNow.AddDays(2),
            totalSeats: 100,
            description: "Test Description"
        )
        {
            Id = _existingEventId
        };

        _existingBookingId = Guid.NewGuid();

        var testBooking = new Booking(
            _existingEventId
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
        var result = await _bookingService.GetBookingByIdAsync(_existingBookingId, ct);

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
            _bookingService.GetBookingByIdAsync(nonExistingId, ct));

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

using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Domain.Exceptions;
using EventGathera.Events.Infrastructure.DataAccess;
using EventGathera.Events.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventGathera.Events.Tests;

public class EventServiceGetEventByIdTests
{
    private readonly EventsDbContext _dbContext;
    private readonly IEventService _eventService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Guid _techConferenceId;
    private readonly Guid _musicFestivalId;
    private readonly Guid _aiWorkshopId;
    private readonly Mock<ICacheService> _cacheMock;

    public EventServiceGetEventByIdTests()
    {
        _dbName = Guid.NewGuid().ToString();

        _techConferenceId = Guid.NewGuid();
        _musicFestivalId = Guid.NewGuid();
        _aiWorkshopId = Guid.NewGuid();

        var services = new ServiceCollection();

        services.AddDbContext<EventsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        _cacheMock = new Mock<ICacheService>();

        _cacheMock
            .Setup(x => x.GetEventByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Event?)null);

        _cacheMock
            .Setup(x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        services.AddScoped(_ => _cacheMock.Object);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<EventsDbContext>();
        _eventService = _serviceProvider.GetRequiredService<IEventService>();


        _dbContext.Events.AddRange(
        [
            new Event(
                title: "Tech Conference 2026",
                startAt: DateTime.Parse("2026-04-10"),
                endAt: DateTime.Parse("2026-04-12"),
                totalSeats: 100,
                description: "Annual tech conference"
            )
            {
                Id = _techConferenceId
            },
            new Event(
                title: "Music Festival",
                startAt: DateTime.Parse("2026-06-15"),
                endAt: DateTime.Parse("2026-06-18"),
                totalSeats: 200,
                description: "Summer music festival"
            )
            {
                Id = _musicFestivalId
            },
            new Event(
                title: "AI Workshop",
                startAt: DateTime.Parse("2026-05-20"),
                endAt: DateTime.Parse("2026-05-21"),
                totalSeats: 50,
                description: "Artificial intelligence workshop"
            )
            {
                Id = _aiWorkshopId
            }
        ]);

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetEventById_WithValidId_ShouldReturnEvent()
    {
        // Arrange
        Guid validId = _musicFestivalId;

        var expectedEvent = await _dbContext.Events
            .FirstAsync(e => e.Id == validId, cancellationToken: TestContext.Current.CancellationToken);

        _cacheMock
           .Setup(x => x.GetEventByIdAsync(validId))
           .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventService.GetEventByIdAsync(validId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEvent.Id, result.Id);
        Assert.Equal(expectedEvent.Title, result.Title);
        Assert.Equal(expectedEvent.Description, result.Description);
        Assert.Equal(expectedEvent.StartAt, result.StartAt);
        Assert.Equal(expectedEvent.EndAt, result.EndAt);
        Assert.Equal(expectedEvent.TotalSeats, result.TotalSeats);
        Assert.Equal(expectedEvent.AvailableSeats, result.AvailableSeats);

        _cacheMock.Verify(
            x => x.GetEventByIdAsync(validId),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEventById_WithNonExistingId_ShouldThrowResourceNotFoundException()
    {
        // Arrange
        Guid nonExistingId = Guid.NewGuid();

        _cacheMock
           .Setup(x => x.GetEventByIdAsync(nonExistingId))
           .ReturnsAsync((Event?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _eventService.GetEventByIdAsync(nonExistingId));

        Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);

        _cacheMock.Verify(
            x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetEventById_WhenEventInCache_ShouldReturnFromCacheAndNotCallRepository()
    {
        // Arrange
        var eventId = _musicFestivalId;
        var cachedEvent = new Event(
            title: "Cached Music Festival",
            startAt: DateTime.Parse("2026-06-15"),
            endAt: DateTime.Parse("2026-06-18"),
            totalSeats: 200,
            description: "Cached version"
        )
        {
            Id = eventId
        };

        _cacheMock
            .Setup(x => x.GetEventByIdAsync(eventId))
            .ReturnsAsync(cachedEvent);

        // Act
        var result = await _eventService.GetEventByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedEvent.Id, result.Id);
        Assert.Equal(cachedEvent.Title, result.Title);
        Assert.Equal("Cached version", result.Description);

        _cacheMock.Verify(
            x => x.GetEventByIdAsync(eventId),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetEventById_WhenEventNotInCache_ShouldGetFromRepositoryAndSaveToCache()
    {
        // Arrange
        var eventId = _musicFestivalId;

        var expectedEvent = await _dbContext.Events
            .FirstAsync(e => e.Id == eventId, cancellationToken: TestContext.Current.CancellationToken);

        _cacheMock
            .Setup(x => x.GetEventByIdAsync(eventId))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _eventService.GetEventByIdAsync(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEvent.Id, result.Id);
        Assert.Equal(expectedEvent.Title, result.Title);
        Assert.Equal(expectedEvent.Description, result.Description);

        _cacheMock.Verify(
            x => x.GetEventByIdAsync(eventId),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
            Times.Once);
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

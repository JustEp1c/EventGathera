using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Infrastructure.DataAccess;
using EventGathera.Events.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EventGathera.Events.Tests;

public class EventServiceGetTopEventsTests : IDisposable
{
    private readonly EventsDbContext _dbContext;
    private readonly IEventService _eventService;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _dbName;
    private readonly Mock<ICacheService> _cacheMock;

    public EventServiceGetTopEventsTests()
    {
        _dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();

        services.AddDbContext<EventsDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        _cacheMock = new Mock<ICacheService>();

        _cacheMock
            .Setup(x => x.GetTopEvents(It.IsAny<int>()))
            .ReturnsAsync((List<Event>?)null);

        _cacheMock
            .Setup(x => x.SetTopEvents(It.IsAny<List<Event>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        services.AddScoped(_ => _cacheMock.Object);

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<EventsDbContext>();
        _eventService = _serviceProvider.GetRequiredService<IEventService>();

        // Добавляем тестовые события
        _dbContext.Events.AddRange(
        [
            new Event(
                title: "Tech Conference",
                startAt: DateTime.Parse("2026-04-10"),
                endAt: DateTime.Parse("2026-04-12"),
                totalSeats: 100,
                description: "Tech conference"
            )
            {
                AvailableSeats = 10  // 90% sold
            },
            new Event(
                title: "Music Festival",
                startAt: DateTime.Parse("2026-06-15"),
                endAt: DateTime.Parse("2026-06-18"),
                totalSeats: 200,
                description: "Music festival"
            )
            {
                AvailableSeats = 50  // 75% sold
            },
            new Event(
                title: "AI Workshop",
                startAt: DateTime.Parse("2026-05-20"),
                endAt: DateTime.Parse("2026-05-21"),
                totalSeats: 50,
                description: "AI workshop"
            )
            {
                AvailableSeats = 40  // 20% sold
            },
            new Event(
                title: "Art Exhibition",
                startAt: DateTime.Parse("2026-07-01"),
                endAt: DateTime.Parse("2026-07-03"),
                totalSeats: 30,
                description: "Art exhibition"
            )
            {
                AvailableSeats = 0   // 100% sold
            }
        ]);

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetTopEvents_WhenInCache_ShouldReturnFromCacheAndNotCallRepository()
    {
        // Arrange
        var cachedTopEvents = new List<Event>
        {
            new Event("Cached Event 1", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 100, "Desc 1"),
            new Event("Cached Event 2", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), 50, "Desc 2")
        };

        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync(cachedTopEvents);

        // Act
        var result = await _eventService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Cached Event 1", result[0].Title);
        Assert.Equal("Cached Event 2", result[1].Title);

        _cacheMock.Verify(
            x => x.GetTopEvents(10),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetTopEvents(It.IsAny<List<Event>>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopEvents_WhenNotInCache_ShouldGetFromRepositoryAndSaveToCache()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync((List<Event>?)null);

        // Act
        var result = await _eventService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _cacheMock.Verify(
            x => x.GetTopEvents(10),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetTopEvents(It.IsAny<List<Event>>(), 10, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopEvents_ShouldReturnCorrectlySortedTopEvents()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync((List<Event>?)null);

        // Act
        var result = await _eventService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);

        Assert.Equal("Art Exhibition", result[0].Title);      // 100% sold
        Assert.Equal("Tech Conference", result[1].Title);     // 90% sold
        Assert.Equal("Music Festival", result[2].Title);      // 75% sold
        Assert.Equal("AI Workshop", result[3].Title);         // 20% sold
    }

    [Fact]
    public async Task GetTopEvents_ShouldUseDefaultTopCount()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync((List<Event>?)null);

        // Act
        var result = await _eventService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _cacheMock.Verify(
            x => x.GetTopEvents(10),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetTopEvents(It.IsAny<List<Event>>(), 10, It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTopEvents_ShouldCacheOnlyTopEvents()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync((List<Event>?)null);

        // Act
        var result = await _eventService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);  // Все события (меньше 10)

        _cacheMock.Verify(
            x => x.SetTopEvents(
                It.Is<List<Event>>(list => list.Count == 4),
                10,
                It.IsAny<int>()),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTopEvents_WhenNoEvents_ShouldCacheEmptyList()
    {
        // Arrange
        await using var emptyContext = CreateEmptyContext();
        var emptyService = CreateEventServiceWithEmptyDb(emptyContext);

        _cacheMock
            .Setup(x => x.GetTopEvents(10))
            .ReturnsAsync((List<Event>?)null);

        // Act
        var result = await emptyService.GetTopEventsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);

        _cacheMock.Verify(
            x => x.SetTopEvents(
                It.Is<List<Event>>(list => list.Count == 0),
                10,
                It.IsAny<int>()),
            Times.Once);
    }

    public void Dispose()
    {
        _dbContext?.Database.EnsureDeleted();
        _dbContext?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private EventsDbContext CreateEmptyContext()
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EventsDbContext(options);
    }

    private IEventService CreateEventServiceWithEmptyDb(EventsDbContext context)
    {
        var repository = new EventRepository(context);
        return new EventService(repository, _cacheMock.Object);
    }
}

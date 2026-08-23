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

namespace EventGathera.Events.Tests
{
    public class EventServiceDeleteEventTests : IDisposable
    {
        private readonly EventsDbContext _dbContext;
        private readonly IEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Guid _existingEventId;
        private readonly Guid _secondEventId;
        private readonly Guid _thirdEventId;
        private readonly Mock<ICacheService> _cacheMock;

        public EventServiceDeleteEventTests()
        {
            _dbName = Guid.NewGuid().ToString();

            _existingEventId = Guid.NewGuid();
            _secondEventId = Guid.NewGuid();
            _thirdEventId = Guid.NewGuid();

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

            _cacheMock
                .Setup(x => x.RemoveEventByIdAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);

            services.AddScoped(_ => _cacheMock.Object);

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();

            _dbContext = _serviceProvider.GetRequiredService<EventsDbContext>();
            _eventService = _serviceProvider.GetRequiredService<IEventService>();

            _existingEventId = Guid.NewGuid();
            _secondEventId = Guid.NewGuid();
            _thirdEventId = Guid.NewGuid();

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
                    Id = _existingEventId  // Устанавливаем Id отдельно, так как конструктор генерирует новый
                },
                new Event(
                    title: "Music Festival",
                    startAt: DateTime.Parse("2026-06-15"),
                    endAt: DateTime.Parse("2026-06-18"),
                    totalSeats: 200,
                    description: "Summer music festival"
                )
                {
                    Id = _secondEventId
                },
                new Event(
                    title: "AI Workshop",
                    startAt: DateTime.Parse("2026-05-20"),
                    endAt: DateTime.Parse("2026-05-21"),
                    totalSeats: 50,
                    description: "Artificial intelligence workshop"
                )
                {
                    Id = _thirdEventId
                }
            ]);

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task DeleteEvent_WithValidId_ShouldDeleteEvent()
        {
            // Arrange
            Guid validId = _existingEventId;

            // Проверяем, что событие существует до удаления
            var eventBefore = await _dbContext.Events
                .FirstOrDefaultAsync(e => e.Id == validId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(eventBefore);

            // Act
            await _eventService.DeleteEventAsync(validId);

            // Assert
            var eventAfter = await _dbContext.Events
                .FirstOrDefaultAsync(e => e.Id == validId, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Null(eventAfter);

            // Проверяем, что другие события остались
            var remainingEvents = await _dbContext.Events.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(2, remainingEvents.Count);
            Assert.Contains(remainingEvents, e => e.Id == _secondEventId);
            Assert.Contains(remainingEvents, e => e.Id == _thirdEventId);

            _cacheMock.Verify(
                x => x.RemoveEventByIdAsync(validId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteEvent_WithNonExistingId_ShouldThrowResourceNotFoundException()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                _eventService.DeleteEventAsync(nonExistingId));

            Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);

            // Проверяем, что количество событий не изменилось
            var eventsCount = await _dbContext.Events.CountAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(3, eventsCount);

            _cacheMock.Verify(
                x => x.RemoveEventByIdAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteEvent_ShouldInvalidateCache()
        {
            // Arrange
            Guid validId = _existingEventId;

            // Act
            await _eventService.DeleteEventAsync(validId, TestContext.Current.CancellationToken);

            // Assert
            _cacheMock.Verify(
                x => x.RemoveEventByIdAsync(validId),
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
}

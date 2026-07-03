using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Repositories.Implementations;
using EventGathera.Api.Repositories.Interfaces;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Tests
{
    public class EventServiceDeleteEventTests
    {
        private readonly AppDbContext _dbContext;
        private readonly IEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Guid _existingEventId;
        private readonly Guid _secondEventId;
        private readonly Guid _thirdEventId;

        public EventServiceDeleteEventTests()
        {
            _dbName = Guid.NewGuid().ToString();

            _existingEventId = Guid.NewGuid();
            _secondEventId = Guid.NewGuid();
            _thirdEventId = Guid.NewGuid();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();

            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
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

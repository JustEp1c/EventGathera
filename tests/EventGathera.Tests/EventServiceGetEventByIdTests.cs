using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Tests
{
    public class EventServiceGetEventByIdTests
    {
        private readonly AppDbContext _dbContext;
        private readonly IEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Guid _techConferenceId;
        private readonly Guid _musicFestivalId;
        private readonly Guid _aiWorkshopId;

        public EventServiceGetEventByIdTests()
        {
            _dbName = Guid.NewGuid().ToString();

            _techConferenceId = Guid.NewGuid();
            _musicFestivalId = Guid.NewGuid();
            _aiWorkshopId = Guid.NewGuid();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IEventService, EventService>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();

            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
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
        }

        [Fact]
        public async Task GetEventById_WithNonExistingId_ShouldThrowResourceNotFoundException()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                _eventService.GetEventByIdAsync(nonExistingId));

            Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);
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

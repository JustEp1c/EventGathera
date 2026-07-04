using EventGathera.Application.DTO.Requests;
using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Implementations;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace EventGathera.Tests
{
    public class EventServiceCreateEventTests
    {
        private readonly AppDbContext _dbContext;
        private readonly IEventService _eventService; 
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;

        public EventServiceCreateEventTests() 
        {
            _dbName = Guid.NewGuid().ToString();

            var services = new ServiceCollection();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();

            _dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
            _eventService = _serviceProvider.GetRequiredService<IEventService>();
        }

        [Fact]
        public async Task CreateEvent_ShouldReturnCreatedEvent()
        {
            // Arrange
            var request = new EventRequest
            { 
                Title = "Test Event",
                Description = "description",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(1),
                TotalSeats = 100
            };

            // Act
            var result = await _eventService.CreateEventAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Title, result.Title);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.StartAt, result.StartAt);
            Assert.Equal(request.EndAt, result.EndAt);
            Assert.Equal(request.TotalSeats, result.TotalSeats);
            Assert.Equal(request.TotalSeats, result.AvailableSeats);

            var savedEvent = await _dbContext.Events
                .FirstOrDefaultAsync(e => e.Id == result.Id, cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(savedEvent);
            Assert.Equal(result.Title, savedEvent.Title);
        }

        [Fact]
        public async Task CreateEvent_ShouldAssignUniqueIds()
        {
            // Arrange
            var request1 = new EventRequest
            {
                Title = "Event 1",
                Description = "Description 1",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(1),
                TotalSeats = 100
            };

            var request2 = new EventRequest
            {
                Title = "Event 2",
                Description = "Description 2",
                StartAt = DateTime.Now.AddDays(2),
                EndAt = DateTime.Now.AddDays(2).AddHours(1),
                TotalSeats = 100
            };

            // Act
            var event1 = await _eventService.CreateEventAsync(request1);
            var event2 = await _eventService.CreateEventAsync(request2);

            // Assert
            Assert.NotEqual(event1.Id, event2.Id);

            var events = await _dbContext.Events.ToListAsync(cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(2, events.Count);
            Assert.Contains(events, e => e.Id == event1.Id);
            Assert.Contains(events, e => e.Id == event2.Id);
        }

        [Fact]
        public void CreateEvent_WithEndDateEarlierThanStartDate_ShouldHaveValidationError()
        {
            // Arrange
            var request = new EventRequest
            {
                Title = "Invalid Event",
                Description = "Test Description",
                StartAt = DateTime.Parse("2026-12-10"),
                EndAt = DateTime.Parse("2026-12-05"), // End before start
                TotalSeats = 100
            };

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v =>
                v.ErrorMessage == "Время начала события должно быть меньше времени окончания");
        }

        [Fact]
        public void CreateEvent_WithEqualStartAndEndDates_ShouldHaveValidationError()
        {
            // Arrange
            var request = new EventRequest
            {
                Title = "Same Day Event",
                Description = "Test Description",
                StartAt = DateTime.Parse("2026-12-10"),
                EndAt = DateTime.Parse("2026-12-10"), // Equal dates
                TotalSeats = 100
            };

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v =>
                v.ErrorMessage == "Время начала события должно быть меньше времени окончания");
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
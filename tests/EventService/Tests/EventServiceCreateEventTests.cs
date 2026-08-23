using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.DTO.Requests;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using EventGathera.Events.Domain.Entities;
using EventGathera.Events.Infrastructure.DataAccess;
using EventGathera.Events.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace EventGathera.Events.Tests
{
    public class EventServiceCreateEventTests
    {
        private readonly EventsDbContext _dbContext;
        private readonly IEventService _eventService; 
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Mock<ICacheService> _cacheMock;

        public EventServiceCreateEventTests() 
        {
            _dbName = Guid.NewGuid().ToString();

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

            _cacheMock.Verify(
                x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
                Times.Once);
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

            _cacheMock.Verify(
                x => x.SetEventAsync(It.IsAny<Event>(), It.IsAny<int>()),
                Times.Exactly(2));
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

        [Fact]
        public async Task CreateEvent_ShouldSaveEventToCache()
        {
            // Arrange
            var request = new EventRequest
            {
                Title = "Cache Test Event",
                Description = "Testing cache",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(1).AddHours(1),
                TotalSeats = 50
            };

            // Act
            var result = await _eventService.CreateEventAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);

            _cacheMock.Verify(
                x => x.SetEventAsync(
                    It.Is<Event>(e => e.Id == result.Id && e.Title == request.Title),
                    It.IsAny<int>()),
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
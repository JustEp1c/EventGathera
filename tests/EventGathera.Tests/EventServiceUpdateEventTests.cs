using EventGathera.Api.Contracts.DTO.Requests;
using EventGathera.Api.DataAccess;
using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace EventGathera.Tests
{
    public class EventServiceUpdateEventTests
    {
        private readonly AppDbContext _dbContext;
        private readonly IEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Guid _techConferenceId;
        private readonly Guid _musicFestivalId;
        private readonly Guid _aiWorkshopId;

        public EventServiceUpdateEventTests()
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

            _dbContext.Events.AddRange(new[]
            {
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
            });

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task UpdateEvent_WithValidId_ShouldUpdateEventProperties()
        {
            // Arrange
            Guid validId = _techConferenceId;
            var updateRequest = new EventRequest
            {
                Title = "Updated Tech Conference 2026",
                Description = "Updated annual tech conference description",
                StartAt = DateTime.Parse("2026-04-15"),
                EndAt = DateTime.Parse("2026-04-18"),
                TotalSeats = 150 // Увеличиваем количество мест
            };

            // Act
            await _eventService.UpdateEventAsync(validId, updateRequest);

            // Обновляем контекст, чтобы получить актуальные данные
            await _dbContext.Entry(await _dbContext.Events.FindAsync(new object?[] { validId }, TestContext.Current.CancellationToken)).ReloadAsync();
            var updatedEvent = await _dbContext.Events.FirstAsync(e => e.Id == validId, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(updateRequest.Title, updatedEvent.Title);
            Assert.Equal(updateRequest.Description, updatedEvent.Description);
            Assert.Equal(updateRequest.StartAt, updatedEvent.StartAt);
            Assert.Equal(updateRequest.EndAt, updatedEvent.EndAt);
        }

        [Fact]
        public async Task UpdateEvent_WithNonExistingId_ShouldThrowResourceNotFoundException()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();
            var updateRequest = new EventRequest
            {
                Title = "New Title",
                Description = "New Description",
                StartAt = DateTime.Parse("2026-12-01"),
                EndAt = DateTime.Parse("2026-12-02"),
                TotalSeats = 100
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
                _eventService.UpdateEventAsync(nonExistingId, updateRequest));

            Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);
        }

        [Fact]
        public void EventRequest_WithEndDateEarlierThanStartDate_ShouldHaveValidationError()
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
        public void EventRequest_WithEqualStartAndEndDates_ShouldHaveValidationError()
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

    }
}

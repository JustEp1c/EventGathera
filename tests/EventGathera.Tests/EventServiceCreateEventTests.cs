using EventGathera.Api.Contracts.DTO.Requests;
using EventGathera.Api.Services.Implementations;
using System.ComponentModel.DataAnnotations;

namespace EventGathera.Tests
{
    public class EventServiceCreateEventTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;

        public EventServiceCreateEventTests() 
        {
            _eventStorage = new EventStorage();
            _eventService = new EventService(_eventStorage);
        }

        [Fact]
        public void CreateEvent_ShouldReturnCreatedEvent()
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
            var result = _eventService.CreateEvent(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Title, result.Title);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.StartAt, result.StartAt);
            Assert.Equal(request.EndAt, result.EndAt);
            Assert.Contains(result, _eventStorage.Events);
        }

        [Fact]
        public void CreateEvent_ShouldAssignUniqueIds()
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
            var event1 = _eventService.CreateEvent(request1);
            var event2 = _eventService.CreateEvent(request2);

            // Assert
            Assert.NotEqual(event1.Id, event2.Id);
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

    }
}

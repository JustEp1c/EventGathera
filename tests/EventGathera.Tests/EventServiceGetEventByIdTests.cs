using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Tests
{
    public class EventServiceGetEventByIdTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;

        public EventServiceGetEventByIdTests()
        {
            _eventStorage = new EventStorage();

            _eventStorage.Events.AddRange(
            [
                new Event
                {
                    Id = 1,
                    Title = "Tech Conference 2026",
                    Description = "Annual tech conference",
                    StartAt = DateTime.Parse("2026-04-10"),
                    EndAt = DateTime.Parse("2026-04-12")
                },
                new Event
                {
                    Id = 2,
                    Title = "Music Festival",
                    Description = "Summer music festival",
                    StartAt = DateTime.Parse("2026-06-15"),
                    EndAt = DateTime.Parse("2026-06-18")
                },
                new Event
                {
                    Id = 3,
                    Title = "AI Workshop",
                    Description = "Artificial intelligence workshop",
                    StartAt = DateTime.Parse("2026-05-20"),
                    EndAt = DateTime.Parse("2026-05-21")
                }
            ]);

            _eventService = new EventService(_eventStorage);
        }

        [Fact]
        public void GetEventById_WithValidId_ShouldReturnEvent()
        {
            // Arrange
            int validId = 2;
            var expectedEvent = _eventStorage.Events.First(e => e.Id == validId);

            // Act
            var result = _eventService.GetEventById(validId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedEvent.Id, result.Id);
            Assert.Equal(expectedEvent.Title, result.Title);
            Assert.Equal(expectedEvent.Description, result.Description);
            Assert.Equal(expectedEvent.StartAt, result.StartAt);
            Assert.Equal(expectedEvent.EndAt, result.EndAt);
        }

        [Fact]
        public void GetEventById_WithNonExistingId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int nonExiststingId = 999;

            // Act & Assert
            var exception = Assert.Throws<ResourceNotFoundException>(() =>
                _eventService.GetEventById(nonExiststingId));

            Assert.Equal($"Событие с ID {nonExiststingId} не найдено", exception.Message);
        }

    }
}

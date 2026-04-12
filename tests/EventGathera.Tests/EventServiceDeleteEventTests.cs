using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Tests
{
    public class EventServiceDeleteEventTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;

        public EventServiceDeleteEventTests()
        {
            _eventStorage = new EventStorage();

            _eventStorage.Events.AddRange(new[]
            {
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
            });

            _eventService = new EventService(_eventStorage);
        }

        [Fact]
        public void DeleteEvent_WithValidId_ShouldDeleteEvent()
        {
            // Arrange
            int validId = 1;

            // Act
            _eventService.DeleteEvent(validId);

            // Assert
            Assert.DoesNotContain(_eventStorage.Events, e => e.Id == 1);
        }

        [Fact]
        public void DeleteEvent_WithNonExistingId_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            int nonExiststingId = 999;

            // Act & Assert
            var exception = Assert.Throws<ResourceNotFoundException>(() =>
                _eventService.DeleteEvent(nonExiststingId));

            Assert.Equal($"Событие с ID {nonExiststingId} не найдено", exception.Message);
        }
    }
}

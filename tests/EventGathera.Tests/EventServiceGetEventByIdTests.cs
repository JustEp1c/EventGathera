using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;

namespace EventGathera.Tests
{
    public class EventServiceGetEventByIdTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;
        private readonly Guid _techConferenceId;
        private readonly Guid _musicFestivalId;
        private readonly Guid _aiWorkshopId;

        public EventServiceGetEventByIdTests()
        {
            _eventStorage = new EventStorage();

            _techConferenceId = Guid.NewGuid();
            _musicFestivalId = Guid.NewGuid();
            _aiWorkshopId = Guid.NewGuid();

            _eventStorage.Events.AddRange(
            [
                new Event
                {
                    Id = _techConferenceId,
                    Title = "Tech Conference 2026",
                    Description = "Annual tech conference",
                    StartAt = DateTime.Parse("2026-04-10"),
                    EndAt = DateTime.Parse("2026-04-12")
                },
                new Event
                {
                    Id = _musicFestivalId,
                    Title = "Music Festival",
                    Description = "Summer music festival",
                    StartAt = DateTime.Parse("2026-06-15"),
                    EndAt = DateTime.Parse("2026-06-18")
                },
                new Event
                {
                    Id = _aiWorkshopId,
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
            Guid validId = _musicFestivalId;
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
        public void GetEventById_WithNonExistingId_ShouldThrowResourceNotFoundException()
        {
            // Arrange
            Guid nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = Assert.Throws<ResourceNotFoundException>(() =>
                _eventService.GetEventById(nonExistingId));

            Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);
        }

    }
}

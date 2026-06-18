using EventGathera.Api.Domain;
using EventGathera.Api.Exceptions;
using EventGathera.Api.Services.Implementations;
using System.Security.Cryptography;

namespace EventGathera.Tests
{
    public class EventServiceDeleteEventTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;
        private readonly Guid _existingEventId;
        private readonly Guid _secondEventId;
        private readonly Guid _thirdEventId;

        public EventServiceDeleteEventTests()
        {
            _eventStorage = new EventStorage();

            _existingEventId = Guid.NewGuid();
            _secondEventId = Guid.NewGuid();
            _thirdEventId = Guid.NewGuid();

            _eventStorage.Events.AddRange(new[]
            {
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
            });

            _eventService = new EventService(_eventStorage);
        }

        [Fact]
        public void DeleteEvent_WithValidId_ShouldDeleteEvent()
        {
            // Arrange
            Guid validId = _existingEventId;

            // Act
            _eventService.DeleteEvent(validId);

            // Assert
            Assert.DoesNotContain(_eventStorage.Events, e => e.Id == _existingEventId);
        }

        [Fact]
        public void DeleteEvent_WithNonExistingId_ShouldThrowResourceNotFoundException()
        {
            // Arrange
            Guid nonExistingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            // Act & Assert
            var exception = Assert.Throws<ResourceNotFoundException>(() =>
                _eventService.DeleteEvent(nonExistingId));

            Assert.Equal($"Событие с ID {nonExistingId} не найдено", exception.Message);
        }
    }
}

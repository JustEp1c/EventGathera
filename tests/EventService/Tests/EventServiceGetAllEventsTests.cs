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
    public class EventServiceGetAllEventsTests
    {
        private readonly EventsDbContext _dbContext;
        private readonly IEventService _eventService;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _dbName;
        private readonly Dictionary<string, Guid> _eventIds;
        private readonly Mock<ICacheService> _cacheMock;

        public EventServiceGetAllEventsTests()
        {
            _dbName = Guid.NewGuid().ToString();
            _eventIds = new Dictionary<string, Guid>();

            _eventIds["TechConference"] = Guid.NewGuid();
            _eventIds["MusicFestival"] = Guid.NewGuid();
            _eventIds["AIWorkshop"] = Guid.NewGuid();
            _eventIds["TechMeetup"] = Guid.NewGuid();
            _eventIds["DataScienceSummit"] = Guid.NewGuid();

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
                    Id = _eventIds["TechConference"]
                },
                new Event(
                    title: "Music Festival",
                    startAt: DateTime.Parse("2026-06-15"),
                    endAt: DateTime.Parse("2026-06-18"),
                    totalSeats: 200,
                    description: "Summer music festival"
                )
                {
                    Id = _eventIds["MusicFestival"]
                },
                new Event(
                    title: "AI Workshop",
                    startAt: DateTime.Parse("2026-05-20"),
                    endAt: DateTime.Parse("2026-05-21"),
                    totalSeats: 50,
                    description: "Artificial intelligence workshop"
                )
                {
                    Id = _eventIds["AIWorkshop"]
                },
                new Event(
                    title: "Tech Meetup",
                    startAt: DateTime.Parse("2026-04-25"),
                    endAt: DateTime.Parse("2026-04-25"),
                    totalSeats: 30,
                    description: "Local tech community meetup"
                )
                {
                    Id = _eventIds["TechMeetup"]
                },
                new Event(
                    title: "Data Science Summit",
                    startAt: DateTime.Parse("2026-07-10"),
                    endAt: DateTime.Parse("2026-07-12"),
                    totalSeats: 150,
                    description: "Big data and analytics conference"
                )
                {
                    Id = _eventIds["DataScienceSummit"]
                }
            ]);

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task GetAllEvents_WithNoQueryParams_ShouldReturnPaginatedResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var totalEvents = await _dbContext.Events.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Act
            var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(totalEvents, result.TotalItems);
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(totalEvents, result.ItemsOnCurrrentPage);
            Assert.Equal(totalEvents, result.Items.Count());
            Assert.All(result.Items, item => Assert.Contains(item, _dbContext.Events));
        }

        [Fact]
        public async Task GetAllEvents_WithTitleFilter_ShouldReturnOnlyMatchingEvents()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Tech Conference и Tech Meetup
            Assert.Equal(2, result.Items.Count());
            Assert.All(result.Items, item => Assert.Contains("Tech", item.Title, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(result.Items, item => item.Title == "Music Festival");
            Assert.DoesNotContain(result.Items, item => item.Title == "AI Workshop");
            Assert.DoesNotContain(result.Items, item => item.Title == "Data Science Summit");
        }
        [Fact]
        public async Task GetAllEvents_WithTitleFilter_CaseInsensitive_ShouldWork()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "tech",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.Equal(2, result.TotalItems);
            Assert.All(result.Items, item =>
                Assert.Contains("Tech", item.Title, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetAllEvents_WithTitleFilter_NoMatches_ShouldReturnEmptyResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "NonExistingEvent",
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalItems);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public async Task GetAllEvents_WithFromDateFilter_ShouldReturnEventsStartingAfterDate()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                From = DateTime.Parse("2026-05-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalItems); // AI Workshop (20.05), Music Festival (15.06), Data Science Summit (10.07)
            Assert.All(result.Items, item => Assert.True(item.StartAt >= queryParams.From.Value));
            Assert.DoesNotContain(result.Items, item => item.Id == _eventIds["TechConference"] || item.Id == _eventIds["TechMeetup"]); // Tech Conference и Tech Meetup
        }

        [Fact]
        public async Task GetAllEvents_WithToDateFilter_ShouldReturnEventsEndingBeforeDate()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                To = DateTime.Parse("2026-05-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Tech Conference (12.04) и Tech Meetup (25.04)
            Assert.All(result.Items, item => Assert.True(item.EndAt <= queryParams.To.Value));
            Assert.DoesNotContain(result.Items, item =>
                item.Id == _eventIds["AIWorkshop"] ||
                item.Id == _eventIds["MusicFestival"] ||
                item.Id == _eventIds["DataScienceSummit"]);
        }

        [Fact]
        public async Task GetAllEvents_WithFromAndToDateFilter_ShouldReturnEventsInDateRange()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                From = DateTime.Parse("2026-04-20"),
                To = DateTime.Parse("2026-06-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Tech Meetup (25.04) и AI Workshop (20-21.05)
            Assert.All(result.Items, item =>
            {
                Assert.True(item.StartAt >= queryParams.From.Value);
                Assert.True(item.EndAt <= queryParams.To.Value);
            });
        }

        [Fact]
        public async Task GetAllEvents_WithPagination_FirstPage_ShouldReturnFirstPageItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 2
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(2, result.ItemsOnCurrrentPage);
            Assert.Equal(_eventIds["TechConference"], result.Items.ElementAt(0).Id);
            Assert.Equal(_eventIds["TechMeetup"], result.Items.ElementAt(1).Id);
        }

        [Fact]
        public async Task GetAllEvents_WithPagination_SecondPage_ShouldReturnSecondPageItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 2,
                PageSize = 2
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2, result.CurrrentPage);
            Assert.Equal(2, result.ItemsOnCurrrentPage);
            Assert.Equal(_eventIds["AIWorkshop"], result.Items.ElementAt(0).Id);
            Assert.Equal(_eventIds["MusicFestival"], result.Items.ElementAt(1).Id);
        }

        [Fact]
        public async Task GetAllEvents_WithPagination_LastPage_WithPartialItems_ShouldReturnRemainingItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 3,
                PageSize = 2
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Single(result.Items);
            Assert.Equal(3, result.CurrrentPage);
            Assert.Equal(1, result.ItemsOnCurrrentPage);
            Assert.Equal(_eventIds["DataScienceSummit"], result.Items.ElementAt(0).Id);
        }

        [Fact]
        public async Task GetAllEvents_WithPagination_PageBeyondTotal_ShouldReturnEmptyResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 10,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Empty(result.Items);
            Assert.Equal(10, result.CurrrentPage);
            Assert.Equal(0, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public async Task GetAllEvents_WithPagination_PageSizeLargerThanTotal_ShouldReturnAllItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 100
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(5, result.Items.Count());
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(5, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public async Task GetAllEvents_WithCombinedFilters_TitleAndDateRange_ShouldReturnMatchingEvents()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                From = DateTime.Parse("2026-04-01"),
                To = DateTime.Parse("2026-05-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Tech Conference и Tech Meetup
            Assert.All(result.Items, item =>
            {
                Assert.Contains("Tech", item.Title, StringComparison.OrdinalIgnoreCase);
                Assert.True(item.StartAt >= queryParams.From.Value);
                Assert.True(item.EndAt <= queryParams.To.Value);
            });
        }

        [Fact]
        public async Task GetAllEvents_WithCombinedFilters_TitleAndFromDate_ShouldReturnCorrectEvents()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                From = DateTime.Parse("2026-04-20"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems); // Только Tech Meetup (25.04)
            Assert.Equal("Tech Meetup", result.Items.ElementAt(0).Title);
            Assert.True(result.Items.ElementAt(0).StartAt >= queryParams.From.Value);
        }

        [Fact]
        public async Task GetAllEvents_WithCombinedFilters_AllFiltersApplied_ShouldReturnPreciseResults()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "a", // Будет искать 'a' в названии
                From = DateTime.Parse("2026-04-01"),
                To = DateTime.Parse("2026-08-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            // Должны найти события с буквой 'a' в названии: 
            // Music Festival, AI Workshop, Data Science Summit (4 события)
            Assert.Equal(3, result.TotalItems);
            Assert.All(result.Items, item =>
            {
                Assert.Contains("a", item.Title, StringComparison.OrdinalIgnoreCase);
                Assert.True(item.StartAt >= queryParams.From.Value);
                Assert.True(item.EndAt <= queryParams.To.Value);
            });
        }

        [Fact]
        public async Task GetAllEvents_WithCombinedFiltersAndPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                Page = 2,
                PageSize = 1
            };

            // Act
                    var result = await _eventService.GetAllEventsAsync(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Всего 2 Tech события
            Assert.Single(result.Items);
            Assert.Equal(2, result.CurrrentPage);
            Assert.Equal(1, result.ItemsOnCurrrentPage);
            Assert.Equal("Tech Meetup", result.Items.ElementAt(0).Title);
        }

        [Fact]
        public void GetAllEvents_WithEqualFromAndToParams_ShouldHaveValidationError()
        {
            // Arrange
            var request = new EventQueryParams
            {
                Title = "Same Day Event",
                From = DateTime.Parse("2026-12-10"),
                To = DateTime.Parse("2026-12-10") // Equal dates
            };

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v =>
                v.ErrorMessage == "Дата начала фильтрации не может быть позже даты окончания фильтрации");
        }

        [Fact]
        public void GetAllEvents_WithToEarlierThanFromParams_ShouldHaveValidationError()
        {
            // Arrange
            var request = new EventQueryParams
            {
                Title = "Invalid Event",
                From = DateTime.Parse("2026-12-10"),
                To = DateTime.Parse("2026-12-09") // From before To
            };

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Contains(validationResults, v =>
                v.ErrorMessage == "Дата начала фильтрации не может быть позже даты окончания фильтрации");
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

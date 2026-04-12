using EventGathera.Api.Domain;
using EventGathera.Api.DTO.Requests;
using EventGathera.Api.DTO.Responses;
using EventGathera.Api.Services.Implementations;
using System.ComponentModel.DataAnnotations;

namespace EventGathera.Tests
{
    public class EventServiceGetAllEventsTests
    {
        private readonly EventService _eventService;
        private readonly EventStorage _eventStorage;

        public EventServiceGetAllEventsTests()
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
                },
                new Event
                {
                    Id = 4,
                    Title = "Tech Meetup",
                    Description = "Local tech community meetup",
                    StartAt = DateTime.Parse("2026-04-25"),
                    EndAt = DateTime.Parse("2026-04-25")
                },
                new Event
                {
                    Id = 5,
                    Title = "Data Science Summit",
                    Description = "Big data and analytics conference",
                    StartAt = DateTime.Parse("2026-07-10"),
                    EndAt = DateTime.Parse("2026-07-12")
                }
            ]);

            _eventService = new EventService(_eventStorage);
        }

        [Fact]
        public void GetAllEvents_WithNoQueryParams_ShouldReturnPaginatedResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 10
            };

            var expectedPaginatedResult = new PaginatedResult<Event>
            {
                TotalItems = _eventStorage.Events.Count,
            };
            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_eventStorage.Events.Count, result.TotalItems);
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(_eventStorage.Events.Count, result.ItemsOnCurrrentPage);
            Assert.Equal(_eventStorage.Events.Count, result.Items.Count());
            Assert.All(result.Items, item => Assert.Contains(item, _eventStorage.Events));
        }

        [Fact]
        public void GetAllEvents_WithTitleFilter_ShouldReturnOnlyMatchingEvents()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

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
        public void GetAllEvents_WithTitleFilter_CaseInsensitive_ShouldWork()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "tech",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.Equal(2, result.TotalItems);
            Assert.All(result.Items, item =>
                Assert.Contains("Tech", item.Title, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GetAllEvents_WithTitleFilter_NoMatches_ShouldReturnEmptyResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "NonExistingEvent",
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalItems);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public void GetAllEvents_WithFromDateFilter_ShouldReturnEventsStartingAfterDate()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                From = DateTime.Parse("2026-05-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalItems); // AI Workshop (20.05), Music Festival (15.06), Data Science Summit (10.07)
            Assert.All(result.Items, item => Assert.True(item.StartAt >= queryParams.From.Value));
            Assert.DoesNotContain(result.Items, item => item.Id == 1 || item.Id == 4); // Tech Conference и Tech Meetup
        }

        [Fact]
        public void GetAllEvents_WithToDateFilter_ShouldReturnEventsEndingBeforeDate()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                To = DateTime.Parse("2026-05-01"),
                Page = 1,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalItems); // Tech Conference (12.04) и Tech Meetup (25.04)
            Assert.All(result.Items, item => Assert.True(item.EndAt <= queryParams.To.Value));
            Assert.DoesNotContain(result.Items, item => item.Id == 3 || item.Id == 2 || item.Id == 5);
        }

        [Fact]
        public void GetAllEvents_WithFromAndToDateFilter_ShouldReturnEventsInDateRange()
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
            var result = _eventService.GetAllEvents(queryParams);

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
        public void GetAllEvents_WithPagination_FirstPage_ShouldReturnFirstPageItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 2
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(2, result.ItemsOnCurrrentPage);
            Assert.Equal(1, result.Items.ElementAt(0).Id);
            Assert.Equal(2, result.Items.ElementAt(1).Id);
        }

        [Fact]
        public void GetAllEvents_WithPagination_SecondPage_ShouldReturnSecondPageItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 2,
                PageSize = 2
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(2, result.CurrrentPage);
            Assert.Equal(2, result.ItemsOnCurrrentPage);
            Assert.Equal(3, result.Items.ElementAt(0).Id);
            Assert.Equal(4, result.Items.ElementAt(1).Id);
        }

        [Fact]
        public void GetAllEvents_WithPagination_LastPage_WithPartialItems_ShouldReturnRemainingItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 3,
                PageSize = 2
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Single(result.Items);
            Assert.Equal(3, result.CurrrentPage);
            Assert.Equal(1, result.ItemsOnCurrrentPage);
            Assert.Equal(5, result.Items.ElementAt(0).Id);
        }

        [Fact]
        public void GetAllEvents_WithPagination_PageBeyondTotal_ShouldReturnEmptyResult()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 10,
                PageSize = 10
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Empty(result.Items);
            Assert.Equal(10, result.CurrrentPage);
            Assert.Equal(0, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public void GetAllEvents_WithPagination_PageSizeLargerThanTotal_ShouldReturnAllItems()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Page = 1,
                PageSize = 100
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalItems);
            Assert.Equal(5, result.Items.Count());
            Assert.Equal(1, result.CurrrentPage);
            Assert.Equal(5, result.ItemsOnCurrrentPage);
        }

        [Fact]
        public void GetAllEvents_WithCombinedFilters_TitleAndDateRange_ShouldReturnMatchingEvents()
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
            var result = _eventService.GetAllEvents(queryParams);

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
        public void GetAllEvents_WithCombinedFilters_TitleAndFromDate_ShouldReturnCorrectEvents()
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
            var result = _eventService.GetAllEvents(queryParams);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalItems); // Только Tech Meetup (25.04)
            Assert.Equal("Tech Meetup", result.Items.ElementAt(0).Title);
            Assert.True(result.Items.ElementAt(0).StartAt >= queryParams.From.Value);
        }

        [Fact]
        public void GetAllEvents_WithCombinedFilters_AllFiltersApplied_ShouldReturnPreciseResults()
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
            var result = _eventService.GetAllEvents(queryParams);

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
        public void GetAllEvents_WithCombinedFiltersAndPagination_ShouldReturnCorrectPage()
        {
            // Arrange
            var queryParams = new EventQueryParams
            {
                Title = "Tech",
                Page = 2,
                PageSize = 1
            };

            // Act
            var result = _eventService.GetAllEvents(queryParams);

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
    }
}

using System.ComponentModel.DataAnnotations;

namespace EventGathera.Events.Application.DTO.Requests
{
    public class EventQueryParams : IValidatableObject
    {
        public string? Title { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100")]
        public int PageSize { get; set; } = 10;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (From.HasValue && To.HasValue && From.Value >= To.Value)
            {
                yield return new ValidationResult(
                    "Дата начала фильтрации не может быть позже даты окончания фильтрации",
                    [nameof(To)]);
            }
        }
    }
}

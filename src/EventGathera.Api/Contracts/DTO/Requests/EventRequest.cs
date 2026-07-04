using System.ComponentModel.DataAnnotations;

namespace EventGathera.Presentation.Contracts.DTO.Requests;


/// <summary>
/// DTO для создания/обновления события
/// </summary>
public record EventRequest : IValidatableObject
{
    /// <summary>
    /// Название
    /// </summary>
    [Required(ErrorMessage = "Название события обязательно для заполнения")]
    [StringLength(150, ErrorMessage = "Название события должно быть короче 150 символов")]
    public required string Title { get; init; }

    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; init; }

    /// <summary>
    /// Время начала события
    /// </summary>
    [Required(ErrorMessage = "Время начала события обязательно для заполнения")]
    [Range(typeof(DateTime), "2020-01-01", "2050-12-31", ErrorMessage = "Некорректная дата начала события")]
    public required DateTime StartAt { get; init; }

    /// <summary>
    /// Время окончания события
    /// </summary>
    [Required(ErrorMessage = "Время окончания события обязательно для заполнения")]
    [Range(typeof(DateTime), "2020-01-01", "2050-12-31", ErrorMessage = "Некорректная дата окончания события")]
    public required DateTime EndAt { get; init; }

    /// <summary>
    /// Валидация события
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt >= EndAt)
        {
            yield return new ValidationResult(
                "Время начала события должно быть меньше времени окончания",
                [nameof(EndAt)]);
        }

        if (TotalSeats <= 0)
        {
            yield return new ValidationResult(
                "Количество мест на событии должно быть больше 0",
                [nameof(TotalSeats)]);
        }
    }
}

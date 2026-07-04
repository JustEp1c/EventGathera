namespace EventGathera.Presentation.Domain;


/// <summary>
/// Доменная сущность Event (Событие)
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; set; }

    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; set; }

    /// <summary>
    /// Время начала события
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Время окончания события
    /// </summary>
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Навигационное свойство для связи с бронированиями
    /// </summary>
    public List<Booking> Bookings { get; set; }

    public Event(string title, DateTime startAt, DateTime endAt, int totalSeats, string? description = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
    }

    private Event()
    {

    }

    /// <summary>
    /// Проверка и бронирование доступных мест в событии
    /// </summary>
    /// <param name="count">Количество мест для бронирования</param>
    /// <returns></returns>
    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
        {
            return false;
        }

        AvailableSeats -= count;

        return true;
    }

    /// <summary>
    /// Проверка и освобождение занятых мест
    /// </summary>
    /// <param name="count">Количество мест для освобождения</param>
    /// <returns></returns>
    public bool ReleaseSeats(int count = 1)
    {
        int takenSeats = TotalSeats - AvailableSeats;

        if (takenSeats < count)
        {
            return false;
        }

        AvailableSeats += count;

        return true;
    }
}

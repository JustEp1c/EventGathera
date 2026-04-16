namespace EventGathera.Api.Contracts.Enums;

/// <summary>
/// Статус бронирования
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// Ожидание
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Подтверждено
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Отклонено
    /// </summary>
    Rejected = 2
}

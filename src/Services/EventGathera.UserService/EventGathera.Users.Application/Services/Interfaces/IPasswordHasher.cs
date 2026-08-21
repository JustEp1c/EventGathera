namespace EventGathera.Users.Application.Services.Interfaces;

/// <summary>
/// Сервис для хеширования и проверки пароля
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Хешировать пароль
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <returns></returns>
    string HashPassword(string password);

    /// <summary>
    /// Проверить соответствие пароля хешу
    /// </summary>
    /// <param name="password">Пароль</param>
    /// <param name="hashedPassword">Хеш пароля</param>
    /// <returns></returns>
    bool VerifyPassword(string password, string hashedPassword);
}

using EventGathera.Domain.Enums;

namespace EventGathera.Application.Services.Interfaces;

/// <summary>
/// Сервис для аутентификации пользователей
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Регистрация пользователя
    /// </summary>
    /// <param name="login">Логин</param>
    /// <param name="password">Пароль</param>
    /// <param name="role">Роль</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task Register(string login, string password, Roles role = Roles.User, CancellationToken ct = default);

    /// <summary>
    /// Вход пользователя
    /// </summary>
    /// <param name="login"></param>
    /// <param name="password">Пароль</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<string> Login(string login, string password, CancellationToken ct = default);
}

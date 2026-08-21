using EventGathera.Users.Domain.Enums;

namespace EventGathera.Users.Domain.Entities;

/// <summary>
/// Пользователь
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Логин
    /// </summary>
    public string Login { get; set; } = null!;

    /// <summary>
    /// Хеш пароля
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Роль
    /// </summary>
    public Roles Role { get; set; }

    private User()
    {

    }

    public User(string login, string passwordHash, Roles role)
    {
        Id = Guid.NewGuid();
        Login = login;
        PasswordHash = passwordHash;
        Role = role;
    }
}

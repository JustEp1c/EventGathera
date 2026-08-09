using EventGathera.Users.Domain.Entities;

namespace EventGathera.Users.Application.Repositories.Interfaces;

/// <summary>
/// Репозиторий для работы с пользователями
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Получить пользователя по логину
    /// </summary>
    /// <param name="login"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Добавить нового пользователя
    /// </summary>
    /// <param name="newUser"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task AddUserAsync(User newUser, CancellationToken ct = default);
}

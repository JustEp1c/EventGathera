using EventGathera.Domain.Enums;

namespace EventGathera.Application.Services.Interfaces;

/// <summary>
/// Компонент для генерации JWT-токена
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Сгенерировать JWT-токен
    /// </summary>
    /// <param name="userId">Id пользователя</param>
    /// <param name="login">Логин</param>
    /// <param name="role">Роль</param>
    /// <returns></returns>
    string GenerateToken(Guid userId, string login, Roles role);
}

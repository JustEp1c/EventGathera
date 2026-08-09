using EventGathera.Users.Application.Repositories.Interfaces;
using EventGathera.Users.Application.Services.Interfaces;
using EventGathera.Users.Domain.Entities;
using EventGathera.Users.Domain.Enums;
using EventGathera.Users.Domain.Exceptions;

namespace EventGathera.Users.Application.Services.Implementations;

/// <summary>
/// <inheritdoc/>
/// </summary>
public class UserService : IUserService
{
    private readonly IPasswordHasher _passwordHasher;

    private readonly IUserRepository _userRepository;

    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public UserService(IPasswordHasher passwordHasher, IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<string> Login(string login, string password, CancellationToken ct)
    {
        var foundUser = await _userRepository.GetUserByLoginAsync(login, ct);

        if (foundUser == null)
        {
            throw new AuthenticationException($"Неверный логин или пароль");
        }

        if (!_passwordHasher.VerifyPassword(password, foundUser.PasswordHash))
        {
            throw new AuthenticationException("Неверный логин или пароль");
        }

        return _jwtTokenGenerator.GenerateToken(foundUser.Id, foundUser.Login, foundUser.Role);
    }

    public async Task Register(string login, string password, Roles role, CancellationToken ct)
    {
        var newUser = new User(login, _passwordHasher.HashPassword(password), role);

        await _userRepository.AddUserAsync(newUser, ct);
    }
}

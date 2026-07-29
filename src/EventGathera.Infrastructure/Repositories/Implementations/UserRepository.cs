using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Domain;
using EventGathera.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace EventGathera.Infrastructure.Repositories.Implementations;

/// <summary>
/// <inheritdoc/>
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _appDbContext;

    public UserRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task AddUserAsync(User newUser, CancellationToken ct = default)
    {
        await _appDbContext.AddAsync(newUser, ct);
    }

    public async Task<User?> GetUserByLoginAsync(string login, CancellationToken ct = default)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower(), cancellationToken: ct);
    }
}

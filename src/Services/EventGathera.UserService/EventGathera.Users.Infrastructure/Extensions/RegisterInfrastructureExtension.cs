using EventGathera.Users.Application.Repositories.Interfaces;
using EventGathera.Users.Application.Services.Interfaces;
using EventGathera.Users.Infrastructure.Authentication;
using EventGathera.Users.Infrastructure.DataAccess;
using EventGathera.Users.Infrastructure.Repositories.Implementations;
using EventGathera.Users.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EventGathera.Users.Infrastructure.Extensions;

public static class RegisterInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddUsersDbContext(configuration);
        services.AddRepositories();

        return services;
    }

    private static IServiceCollection AddUsersDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}

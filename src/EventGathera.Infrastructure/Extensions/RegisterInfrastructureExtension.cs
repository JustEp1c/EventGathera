using EventGathera.Application.Repositories.Interfaces;
using EventGathera.Application.Services.Interfaces;
using EventGathera.Infrastructure.Authentication;
using EventGathera.Infrastructure.BackgroundServices;
using EventGathera.Infrastructure.DataAccess;
using EventGathera.Infrastructure.Repositories.Implementations;
using EventGathera.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EventGathera.Infrastructure.Extensions;

public static class RegisterInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAppDbContext(configuration);
        services.AddRepositories();
        services.AddBackgroundServices();

        return services;
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<BookingProcessingService>();

        return services;
    }

    private static IServiceCollection AddAppDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}

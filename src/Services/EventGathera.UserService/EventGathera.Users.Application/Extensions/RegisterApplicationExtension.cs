using EventGathera.Users.Application.Services.Implementations;
using EventGathera.Users.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Users.Application.Extensions;

public static class RegisterApplicationExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}

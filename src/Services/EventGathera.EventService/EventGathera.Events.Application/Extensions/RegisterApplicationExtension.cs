using EventGathera.Events.Application.Services.Implementations;
using EventGathera.Events.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Events.Application.Extensions;

public static class RegisterApplicationExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}

using EventGathera.Application.Services.Implementations;
using EventGathera.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Application.Extensions;

public static class RegisterApplicationExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

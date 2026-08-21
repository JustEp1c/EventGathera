using EventGathera.Bookings.Application.Services.Implementations;
using EventGathera.Bookings.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventGathera.Bookings.Application.Extensions;

public static class RegisterApplicationExtension
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}

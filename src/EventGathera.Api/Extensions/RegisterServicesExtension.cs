using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;
using System.Reflection;

namespace EventGathera.Api.Extensions;


/// <summary>
/// Статический класс расширения для регистрации сервисов
/// </summary>
public static class RegisterServicesExtension
{
    /// <summary>
    /// Зарегистрировать сервисы
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddSingleton<EventStorage>();
        services.AddSingleton<BookingStorage>();

        return services;
    }

    public static IServiceCollection RegisterPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);
        });

        return services;
    }
}

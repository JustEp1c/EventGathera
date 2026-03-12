using EventGathera.Api.Services.Implementations;
using EventGathera.Api.Services.Interfaces;

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
        services.AddSingleton<EventStorage>();

        return services;
    }
}

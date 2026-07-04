using EventGathera.Presentation.BackgroundServices;
using EventGathera.Presentation.DataAccess;
using EventGathera.Presentation.Repositories.Implementations;
using EventGathera.Presentation.Repositories.Interfaces;
using EventGathera.Presentation.Services.Implementations;
using EventGathera.Presentation.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EventGathera.Presentation.Extensions;


/// <summary>
/// Статический класс расширения для регистрации сервисов
/// </summary>
public static class RegisterServicesExtension
{
    /// <summary>
    /// Зарегистрировать сервисы
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        services.AddHostedService<BookingProcessingService>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
            configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

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

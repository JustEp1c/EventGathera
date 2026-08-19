using Confluent.Kafka;
using EventGathera.Bookings.Application.Kafka;
using EventGathera.Bookings.Application.Repositories.Interfaces;
using EventGathera.Bookings.Infrastructure.BackgroundServices;
using EventGathera.Bookings.Infrastructure.DataAccess;
using EventGathera.Bookings.Infrastructure.Kafka;
using EventGathera.Bookings.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EventGathera.Bookings.Infrastructure.Extensions;

public static class RegisterInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAppDbContext(configuration);
        services.AddRepositories();
        services.AddBackgroundServices();
        services.AddKafka();

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
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IBookingRepository, BookingRepository>();

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services)
    {
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                Acks = Acks.All,
                EnableIdempotence = true,
                MaxInFlight = 1,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 100
            };

            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }
}

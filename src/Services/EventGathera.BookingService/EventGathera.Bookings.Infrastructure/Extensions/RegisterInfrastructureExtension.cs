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
        services.AddKafka(configuration);

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

    private static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
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

        services.AddSingleton<IConsumer<string, string>>(sp =>
        {

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:ConsumerGroup"],
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                AllowAutoCreateTopics = true,
                SessionTimeoutMs = 6000,
                MaxPollIntervalMs = 300000
            };

            return new ConsumerBuilder<string, string>(consumerConfig).Build();
        });

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddHostedService<KafkaEventConsumer>();

        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddHostedService<OutboxRelayService>();

        return services;
    }
}

using Confluent.Kafka;
using EventGathera.Events.Application.Cache;
using EventGathera.Events.Application.Kafka;
using EventGathera.Events.Application.Repositories.Interfaces;
using EventGathera.Events.Infrastructure.BackgroundServices;
using EventGathera.Events.Infrastructure.Cache;
using EventGathera.Events.Infrastructure.DataAccess;
using EventGathera.Events.Infrastructure.Kafka;
using EventGathera.Events.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
namespace EventGathera.Events.Infrastructure.Extensions;

public static class RegisterInfrastructureExtension
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEventsDbContext(configuration);
        services.AddRepositories();
        services.AddKafka(configuration);
        services.AddRedis(configuration);

        return services;
    }

    private static IServiceCollection AddEventsDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }

    private static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
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

        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        services.AddHostedService<KafkaEventConsumer>();

        services.AddHostedService<OutboxRelayService>();

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = new ConfigurationOptions
            {
                EndPoints = { configuration["Redis:EndPoints"] },
                Password = configuration["Redis:Password"],
                ConnectTimeout = 5000,
                SyncTimeout = 3000,
                AbortOnConnectFail = false,
                ConnectRetry = 3
            };

            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }
}

using System.Diagnostics.CodeAnalysis;
using Application.Common.Interfaces;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

// Registro de infraestrutura (DI scanning, MassTransit/Mongo bootstrap), sem lógica de
// negócio — excluído da cobertura.
[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddMessaging(configuration);

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        services.Scan(scan => scan
            .FromApplicationDependencies()
            .AddClasses(c => c.Where(t => t.Name.EndsWith("Gateway")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoSettings>(configuration.GetSection("MongoSettings"));
        services.AddSingleton<MongoContext>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));
        services.AddScoped<ISagaEventPublisher, MassTransitSagaEventPublisher>();

        var rabbitMq = configuration.GetSection("RabbitMq").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<IniciarDiagnosticoConsumer>();
            x.AddConsumer<IniciarExecucaoConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMq.Host, rabbitMq.VirtualHost, h =>
                {
                    h.Username(rabbitMq.Username);
                    h.Password(rabbitMq.Password);
                });

                // Sem UseDelayedRedelivery: exige o plugin "rabbitmq_delayed_message_exchange"
                // no broker, que não está provisionado nem localmente nem no infra-k8s — usar
                // esse middleware quebra a topologia de todos os consumers (erro
                // "unknown exchange type 'x-delayed-message'"). UseMessageRetry (retentativa
                // imediata, sem devolver a mensagem à fila) já cobre o caso comum de falha
                // transitória sem depender de nenhum plugin do broker.
                cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

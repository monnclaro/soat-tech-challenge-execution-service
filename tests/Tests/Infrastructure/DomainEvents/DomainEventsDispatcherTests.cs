using Application.Common.Interfaces;
using Domain.Common.Events;
using FluentAssertions;
using Infrastructure.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure.DomainEvents;

public class DomainEventsDispatcherTests
{
    private sealed record FakeDomainEventA(string Valor) : IDomainEvent
    {
        public DateTime OcurredAt { get; } = DateTime.UtcNow;
    }

    private sealed record FakeDomainEventB : IDomainEvent
    {
        public DateTime OcurredAt { get; } = DateTime.UtcNow;
    }

    private sealed class FakeHandlerA1(List<string> chamadas) : IDomainEventHandler<FakeDomainEventA>
    {
        public Task Handle(FakeDomainEventA domainEvent, CancellationToken cancellationToken)
        {
            chamadas.Add($"A1:{domainEvent.Valor}");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHandlerA2(List<string> chamadas) : IDomainEventHandler<FakeDomainEventA>
    {
        public Task Handle(FakeDomainEventA domainEvent, CancellationToken cancellationToken)
        {
            chamadas.Add($"A2:{domainEvent.Valor}");
            return Task.CompletedTask;
        }
    }

    private static (IDomainEventsDispatcher Dispatcher, List<string> Chamadas) CriarDispatcher()
    {
        var chamadas = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton(chamadas);
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddTransient<IDomainEventHandler<FakeDomainEventA>, FakeHandlerA1>();
        services.AddTransient<IDomainEventHandler<FakeDomainEventA>, FakeHandlerA2>();

        var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IDomainEventsDispatcher>(), chamadas);
    }

    [Fact]
    public async Task DispatchAsync_ComMultiplosHandlersParaOMesmoEvento_DeveChamarTodosOsHandlers()
    {
        var (dispatcher, chamadas) = CriarDispatcher();

        await dispatcher.DispatchAsync([new FakeDomainEventA("x")]);

        chamadas.Should().BeEquivalentTo(["A1:x", "A2:x"]);
    }

    [Fact]
    public async Task DispatchAsync_ComEventoSemHandlerRegistrado_NaoDeveLancar()
    {
        var (dispatcher, chamadas) = CriarDispatcher();

        var acao = async () => await dispatcher.DispatchAsync([new FakeDomainEventB()]);

        await acao.Should().NotThrowAsync();
        chamadas.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_ComMultiplosEventos_DeveDespacharCadaUmNaOrdem()
    {
        var (dispatcher, chamadas) = CriarDispatcher();

        await dispatcher.DispatchAsync([new FakeDomainEventA("1"), new FakeDomainEventA("2")]);

        chamadas.Should().Equal("A1:1", "A2:1", "A1:2", "A2:2");
    }
}

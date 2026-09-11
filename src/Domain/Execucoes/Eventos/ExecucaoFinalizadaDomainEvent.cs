using Domain.Common.Events;
using Domain.Execucoes.Itens;

namespace Domain.Execucoes.Eventos;

// Ainda não publicado — a mensageria (RabbitMQ/MassTransit) chega em um follow-up PR.
// Quando ligado, o OS Service consome este evento para finalizar a OS
// (ver PLANO-FASE-4-MICROSSERVICOS.md, seção 1.3).
public sealed record ExecucaoFinalizadaDomainEvent(
    Guid IdOrdemServico,
    IReadOnlyList<ItemProduto> Produtos
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

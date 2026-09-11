using Domain.Common.Events;
using Domain.Execucoes.Itens;

namespace Domain.Execucoes.Eventos;

// Ainda não publicado — a mensageria (RabbitMQ/MassTransit) chega em um follow-up PR.
// Quando ligado, este evento vira o comando GerarOrcamento consumido pelo OS Service
// (ver PLANO-FASE-4-MICROSSERVICOS.md, seção 1.3).
public sealed record DiagnosticoFinalizadoDomainEvent(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServico> Servicos,
    IReadOnlyList<ItemProduto> Produtos
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

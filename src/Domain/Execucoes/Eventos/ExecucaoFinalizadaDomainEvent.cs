using Domain.Common.Events;
using Domain.Execucoes.Itens;

namespace Domain.Execucoes.Eventos;

// Publicado (via PublicarExecucaoFinalizadaHandler) quando o último serviço da OS
// termina a execução. O OS Service consome o evento equivalente da saga
// (`ExecucaoFinalizada`) para finalizar a OS.
public sealed record ExecucaoFinalizadaDomainEvent(
    Guid IdOrdemServico,
    IReadOnlyList<ItemProduto> Produtos
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

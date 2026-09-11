using Domain.Common.Events;
using Domain.Execucoes.Itens;

namespace Domain.Execucoes.Eventos;

// Publicado (via PublicarDiagnosticoFinalizadoHandler) quando o diagnóstico é
// finalizado. O OS Service consome o evento equivalente da saga
// (`DiagnosticoFinalizado`) e, a partir dele, comanda o Billing Service a gerar
// o orçamento.
public sealed record DiagnosticoFinalizadoDomainEvent(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServico> Servicos,
    IReadOnlyList<ItemProduto> Produtos
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

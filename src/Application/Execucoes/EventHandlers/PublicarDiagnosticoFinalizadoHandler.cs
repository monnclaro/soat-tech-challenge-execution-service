using Application.Common.Interfaces;
using Domain.Execucoes.Eventos;
using Soat.Contracts.Saga;

namespace Application.Execucoes.EventHandlers;

// Passo 2 -> 3 da saga: ao finalizar o diagnóstico, publica o evento para o OS Service
// registrar os itens e comandar o Billing Service a gerar o orçamento — ver
// PLANO-FASE-4-MICROSSERVICOS.md.
internal sealed class PublicarDiagnosticoFinalizadoHandler : IDomainEventHandler<DiagnosticoFinalizadoDomainEvent>
{
    private readonly ISagaEventPublisher _publisher;

    public PublicarDiagnosticoFinalizadoHandler(ISagaEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(DiagnosticoFinalizadoDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var servicos = domainEvent.Servicos
            .Select(s => new ItemServicoDiagnosticado(s.IdServico, s.NomeServico, s.Valor))
            .ToList();

        var produtos = domainEvent.Produtos
            .Select(p => new ItemProdutoDiagnosticado(p.IdProduto, p.NomeProduto, p.ValorUnitario, p.Quantidade))
            .ToList();

        await _publisher.PublicarDiagnosticoFinalizado(domainEvent.IdOrdemServico, servicos, produtos, cancellationToken);
    }
}

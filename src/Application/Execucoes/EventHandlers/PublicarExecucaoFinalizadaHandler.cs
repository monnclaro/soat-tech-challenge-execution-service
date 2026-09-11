using Application.Common.Interfaces;
using Domain.Execucoes.Eventos;

namespace Application.Execucoes.EventHandlers;

// Último passo automático da saga: ao finalizar a execução do último serviço, publica o
// evento para o OS Service finalizar a OS — ver PLANO-FASE-4-MICROSSERVICOS.md.
internal sealed class PublicarExecucaoFinalizadaHandler : IDomainEventHandler<ExecucaoFinalizadaDomainEvent>
{
    private readonly ISagaEventPublisher _publisher;

    public PublicarExecucaoFinalizadaHandler(ISagaEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(ExecucaoFinalizadaDomainEvent domainEvent, CancellationToken cancellationToken) =>
        _publisher.PublicarExecucaoFinalizada(domainEvent.IdOrdemServico, cancellationToken);
}

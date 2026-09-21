using Application.Common.Interfaces;
using Domain.Execucoes.Eventos;

namespace Application.Execucoes.EventHandlers;

// Compensação da saga: ao cancelar a execução, publica o evento certo para o OS
// Service dependendo da fase em que a falha ocorreu — DiagnosticoFalhou (ainda
// diagnosticando) ou ExecucaoFalhou (diagnóstico já concluído ou em execução).
internal sealed class PublicarExecucaoCanceladaHandler : IDomainEventHandler<ExecucaoCanceladaDomainEvent>
{
    private readonly ISagaEventPublisher _publisher;

    public PublicarExecucaoCanceladaHandler(ISagaEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(ExecucaoCanceladaDomainEvent domainEvent, CancellationToken cancellationToken) =>
        domainEvent.DuranteDiagnostico
            ? _publisher.PublicarDiagnosticoFalhou(domainEvent.IdOrdemServico, domainEvent.Motivo, cancellationToken)
            : _publisher.PublicarExecucaoFalhou(domainEvent.IdOrdemServico, domainEvent.Motivo, cancellationToken);
}

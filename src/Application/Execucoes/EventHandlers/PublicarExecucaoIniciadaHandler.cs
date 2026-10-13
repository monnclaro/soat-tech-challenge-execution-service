using Application.Common.Interfaces;
using Domain.Execucoes.Eventos;

namespace Application.Execucoes.EventHandlers;

// Ao primeiro serviço entrar em execução (ação manual, ver
// ExecucaoOrdemServico.IniciarExecucaoServico), publica a confirmação de
// volta pro OS Service avançar a OS para EmExecucao.
internal sealed class PublicarExecucaoIniciadaHandler : IDomainEventHandler<ExecucaoIniciadaDomainEvent>
{
    private readonly ISagaEventPublisher _publisher;

    public PublicarExecucaoIniciadaHandler(ISagaEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(ExecucaoIniciadaDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await _publisher.PublicarExecucaoIniciada(domainEvent.IdOrdemServico, cancellationToken);
    }
}

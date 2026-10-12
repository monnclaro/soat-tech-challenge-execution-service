using Application.Common.Interfaces;
using Domain.Execucoes.Eventos;

namespace Application.Execucoes.EventHandlers;

// Passo 1 -> 2 da saga: ao abrir a fila de execução (comando IniciarDiagnostico
// recebido do OS Service), publica a confirmação de volta pro OS Service avançar
// a OS para EmDiagnostico.
internal sealed class PublicarDiagnosticoIniciadoHandler : IDomainEventHandler<DiagnosticoIniciadoDomainEvent>
{
    private readonly ISagaEventPublisher _publisher;

    public PublicarDiagnosticoIniciadoHandler(ISagaEventPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(DiagnosticoIniciadoDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        await _publisher.PublicarDiagnosticoIniciado(domainEvent.IdOrdemServico, cancellationToken);
    }
}

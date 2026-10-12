using Domain.Common.Events;

namespace Domain.Execucoes.Eventos;

// Publicado (via PublicarDiagnosticoIniciadoHandler) quando a fila de execução
// abre e confirma o início do diagnóstico. O OS Service consome o evento
// equivalente da saga (`DiagnosticoIniciado`) para só então avançar a OS de
// Recebida para EmDiagnostico — a transição é reativa, não otimista.
public sealed record DiagnosticoIniciadoDomainEvent(Guid IdOrdemServico) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

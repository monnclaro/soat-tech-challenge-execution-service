using Domain.Common.Events;

namespace Domain.Execucoes.Eventos;

// Publicado (via PublicarExecucaoIniciadaHandler) quando o primeiro serviço
// entra em execução. O OS Service consome o evento equivalente da saga
// (`ExecucaoIniciada`) para só então avançar a OS de AguardandoExecucao para
// EmExecucao — a transição é reativa, não otimista (mesmo padrão do
// diagnóstico, ver DiagnosticoIniciadoDomainEvent).
public sealed record ExecucaoIniciadaDomainEvent(Guid IdOrdemServico) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

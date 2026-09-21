using Domain.Common.Events;

namespace Domain.Execucoes.Eventos;

// Publicado (via PublicarExecucaoCanceladaHandler) quando Cancelar() é chamado —
// caminho de compensação da saga. DuranteDiagnostico distingue se a falha ainda
// estava na fase de diagnóstico (o handler publica o evento DiagnosticoFalhou já
// existente) ou já em/depois da execução (publica o evento ExecucaoFalhou).
public sealed record ExecucaoCanceladaDomainEvent(
    Guid IdOrdemServico,
    string Motivo,
    bool DuranteDiagnostico
) : IDomainEvent
{
    public DateTime OcurredAt { get; } = DateTime.UtcNow;
}

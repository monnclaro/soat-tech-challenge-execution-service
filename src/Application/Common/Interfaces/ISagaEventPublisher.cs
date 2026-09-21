using Soat.Contracts.Saga;

namespace Application.Common.Interfaces;

// Port de saída da saga (Application não conhece RabbitMQ/MassTransit — só
// este contrato; a implementação real mora em Infrastructure.Messaging).
public interface ISagaEventPublisher
{
    Task PublicarDiagnosticoFinalizado(
        Guid idOrdemServico,
        IReadOnlyList<ItemServicoDiagnosticado> servicos,
        IReadOnlyList<ItemProdutoDiagnosticado> produtos,
        CancellationToken ct = default);

    Task PublicarDiagnosticoFalhou(Guid idOrdemServico, string motivo, CancellationToken ct = default);

    Task PublicarExecucaoFinalizada(Guid idOrdemServico, CancellationToken ct = default);

    Task PublicarExecucaoFalhou(Guid idOrdemServico, string motivo, CancellationToken ct = default);
}

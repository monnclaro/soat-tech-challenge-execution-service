using Application.Common.Interfaces;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging;

public class MassTransitSagaEventPublisher : ISagaEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitSagaEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublicarDiagnosticoFinalizado(
        Guid idOrdemServico,
        IReadOnlyList<ItemServicoDiagnosticado> servicos,
        IReadOnlyList<ItemProdutoDiagnosticado> produtos,
        CancellationToken ct = default) =>
        _publishEndpoint.Publish(new DiagnosticoFinalizado(idOrdemServico, servicos, produtos), ct);

    public Task PublicarDiagnosticoFalhou(Guid idOrdemServico, string motivo, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new DiagnosticoFalhou(idOrdemServico, motivo), ct);

    public Task PublicarExecucaoFinalizada(Guid idOrdemServico, CancellationToken ct = default) =>
        _publishEndpoint.Publish(new ExecucaoFinalizada(idOrdemServico), ct);
}

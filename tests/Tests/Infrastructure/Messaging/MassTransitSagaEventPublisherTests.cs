using Infrastructure.Messaging;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging;

public class MassTransitSagaEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpoint = new();
    private readonly MassTransitSagaEventPublisher _publisher;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public MassTransitSagaEventPublisherTests()
    {
        _publisher = new MassTransitSagaEventPublisher(_publishEndpoint.Object);
    }

    [Fact]
    public async Task PublicarDiagnosticoFinalizado_DevePublicarEventoComOsMesmosDados()
    {
        var servicos = new List<ItemServicoDiagnosticado> { new(Guid.NewGuid(), "Troca de óleo", 100m) };
        var produtos = new List<ItemProdutoDiagnosticado> { new(Guid.NewGuid(), "Filtro de óleo", 50m, 2m) };

        await _publisher.PublicarDiagnosticoFinalizado(IdOrdemServico, servicos, produtos);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<DiagnosticoFinalizado>(e => e.IdOrdemServico == IdOrdemServico && e.Servicos == servicos && e.Produtos == produtos),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicarDiagnosticoFalhou_DevePublicarEventoComMotivo()
    {
        await _publisher.PublicarDiagnosticoFalhou(IdOrdemServico, "Veículo não atendível");

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<DiagnosticoFalhou>(e => e.IdOrdemServico == IdOrdemServico && e.Motivo == "Veículo não atendível"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublicarExecucaoFinalizada_DevePublicarEventoComIdOrdemServico()
    {
        await _publisher.PublicarExecucaoFinalizada(IdOrdemServico);

        _publishEndpoint.Verify(p => p.Publish(
            It.Is<ExecucaoFinalizada>(e => e.IdOrdemServico == IdOrdemServico),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

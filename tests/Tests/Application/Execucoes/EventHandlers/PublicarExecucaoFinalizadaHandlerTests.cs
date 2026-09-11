using Application.Common.Interfaces;
using Application.Execucoes.EventHandlers;
using Domain.Execucoes.Eventos;
using Domain.Execucoes.Itens;
using Moq;

namespace Tests.Application.Execucoes.EventHandlers;

public class PublicarExecucaoFinalizadaHandlerTests
{
    [Fact]
    public async Task Handle_DevePublicarExecucaoFinalizadaComIdOrdemServico()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarExecucaoFinalizadaHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var produto = new ItemProduto(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);
        var domainEvent = new ExecucaoFinalizadaDomainEvent(idOrdemServico, [produto]);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarExecucaoFinalizada(idOrdemServico, It.IsAny<CancellationToken>()), Times.Once);
    }
}

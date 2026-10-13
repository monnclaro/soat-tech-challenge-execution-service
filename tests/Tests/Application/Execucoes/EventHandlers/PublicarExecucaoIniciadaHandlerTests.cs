using Application.Common.Interfaces;
using Application.Execucoes.EventHandlers;
using Domain.Execucoes.Eventos;
using Moq;

namespace Tests.Application.Execucoes.EventHandlers;

public class PublicarExecucaoIniciadaHandlerTests
{
    [Fact]
    public async Task Handle_DevePublicarExecucaoIniciada()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarExecucaoIniciadaHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var domainEvent = new ExecucaoIniciadaDomainEvent(idOrdemServico);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarExecucaoIniciada(idOrdemServico, It.IsAny<CancellationToken>()), Times.Once);
    }
}

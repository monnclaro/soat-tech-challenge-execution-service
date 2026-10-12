using Application.Common.Interfaces;
using Application.Execucoes.EventHandlers;
using Domain.Execucoes.Eventos;
using Moq;

namespace Tests.Application.Execucoes.EventHandlers;

public class PublicarDiagnosticoIniciadoHandlerTests
{
    [Fact]
    public async Task Handle_DevePublicarDiagnosticoIniciado()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarDiagnosticoIniciadoHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var domainEvent = new DiagnosticoIniciadoDomainEvent(idOrdemServico);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarDiagnosticoIniciado(idOrdemServico, It.IsAny<CancellationToken>()), Times.Once);
    }
}

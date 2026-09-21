using Application.Common.Interfaces;
using Application.Execucoes.EventHandlers;
using Domain.Execucoes.Eventos;
using Moq;

namespace Tests.Application.Execucoes.EventHandlers;

public class PublicarExecucaoCanceladaHandlerTests
{
    [Fact]
    public async Task Handle_QuandoDuranteDiagnostico_DevePublicarDiagnosticoFalhou()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarExecucaoCanceladaHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var domainEvent = new ExecucaoCanceladaDomainEvent(idOrdemServico, "Veículo não atendível", DuranteDiagnostico: true);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarDiagnosticoFalhou(idOrdemServico, "Veículo não atendível", It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublicarExecucaoFalhou(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QuandoForaDoDiagnostico_DevePublicarExecucaoFalhou()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarExecucaoCanceladaHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var domainEvent = new ExecucaoCanceladaDomainEvent(idOrdemServico, "Peça indisponível", DuranteDiagnostico: false);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarExecucaoFalhou(idOrdemServico, "Peça indisponível", It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(p => p.PublicarDiagnosticoFalhou(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

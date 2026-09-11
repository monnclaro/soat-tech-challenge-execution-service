using Application.Common.Interfaces;
using Application.Execucoes.EventHandlers;
using Domain.Execucoes.Eventos;
using Domain.Execucoes.Itens;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Application.Execucoes.EventHandlers;

public class PublicarDiagnosticoFinalizadoHandlerTests
{
    [Fact]
    public async Task Handle_DeveMapearItensEPublicarDiagnosticoFinalizado()
    {
        var publisher = new Mock<ISagaEventPublisher>();
        var handler = new PublicarDiagnosticoFinalizadoHandler(publisher.Object);

        var idOrdemServico = Guid.NewGuid();
        var servico = new ItemServico(Guid.NewGuid(), "Troca de óleo", 100m);
        var produto = new ItemProduto(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);

        var domainEvent = new DiagnosticoFinalizadoDomainEvent(idOrdemServico, [servico], [produto]);

        await handler.Handle(domainEvent, CancellationToken.None);

        publisher.Verify(p => p.PublicarDiagnosticoFinalizado(
            idOrdemServico,
            It.Is<IReadOnlyList<ItemServicoDiagnosticado>>(l =>
                l.Count == 1 && l[0].IdServico == servico.IdServico && l[0].NomeServico == "Troca de óleo" && l[0].Valor == 100m),
            It.Is<IReadOnlyList<ItemProdutoDiagnosticado>>(l =>
                l.Count == 1 && l[0].IdProduto == produto.IdProduto && l[0].NomeProduto == "Filtro de óleo" && l[0].Quantidade == 2m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

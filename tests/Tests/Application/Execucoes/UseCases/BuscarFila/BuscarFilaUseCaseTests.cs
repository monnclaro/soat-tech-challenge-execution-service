using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarFila;
using Domain.Execucoes;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.BuscarFila;

public class BuscarFilaUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<IBuscarFilaOutputPort> _outputPort = new();
    private readonly BuscarFilaUseCase _useCase;

    public BuscarFilaUseCaseTests()
    {
        _useCase = new BuscarFilaUseCase(_gateway.Object, _outputPort.Object);
    }

    [Fact]
    public async Task Execute_SemRegistrosNaFila_DeveNotificarListaVazia()
    {
        _gateway.Setup(g => g.BuscarFila(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExecucaoOrdemServico>)[]);

        await _useCase.Execute(new BuscarFilaInput());

        _outputPort.Verify(o => o.Ok(It.Is<IReadOnlyList<ExecucaoOutput>>(l => l.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task Execute_ComRegistrosNaFila_DeveMapearENotificarTodos()
    {
        var execucao1 = ExecucaoOrdemServico.Abrir(Guid.NewGuid());
        var execucao2 = ExecucaoOrdemServico.Abrir(Guid.NewGuid());

        _gateway.Setup(g => g.BuscarFila(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<ExecucaoOrdemServico>)[execucao1, execucao2]);

        IReadOnlyList<ExecucaoOutput>? captured = null;
        _outputPort.Setup(o => o.Ok(It.IsAny<IReadOnlyList<ExecucaoOutput>>()))
            .Callback<IReadOnlyList<ExecucaoOutput>>(l => captured = l);

        await _useCase.Execute(new BuscarFilaInput());

        captured.Should().NotBeNull();
        captured!.Select(o => o.IdOrdemServico).Should().BeEquivalentTo([execucao1.IdOrdemServico, execucao2.IdOrdemServico]);
    }
}

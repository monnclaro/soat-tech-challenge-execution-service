using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarPorOrdemServico;
using Domain.Execucoes;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.BuscarPorOrdemServico;

public class BuscarPorOrdemServicoUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<IBuscarPorOrdemServicoOutputPort> _outputPort = new();
    private readonly BuscarPorOrdemServicoUseCase _useCase;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public BuscarPorOrdemServicoUseCaseTests()
    {
        _useCase = new BuscarPorOrdemServicoUseCase(_gateway.Object, _outputPort.Object);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoNaoExiste_DeveNotificarNaoEncontrado()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        await _useCase.Execute(new BuscarPorOrdemServicoInput(IdOrdemServico));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoExiste_DeveNotificarOkComOutputMapeado()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        ExecucaoOutput? captured = null;
        _outputPort.Setup(o => o.Ok(It.IsAny<ExecucaoOutput>())).Callback<ExecucaoOutput>(o => captured = o);

        await _useCase.Execute(new BuscarPorOrdemServicoInput(IdOrdemServico));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Never);
        captured.Should().NotBeNull();
        captured!.IdOrdemServico.Should().Be(IdOrdemServico);
    }
}

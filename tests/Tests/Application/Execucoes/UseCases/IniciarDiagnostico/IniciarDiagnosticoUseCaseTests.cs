using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Domain.Common.Exceptions;
using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.IniciarDiagnostico;

public class IniciarDiagnosticoUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<IIniciarDiagnosticoOutputPort> _outputPort = new();
    private readonly IniciarDiagnosticoUseCase _useCase;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public IniciarDiagnosticoUseCaseTests()
    {
        _useCase = new IniciarDiagnosticoUseCase(_gateway.Object, _outputPort.Object);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoNaoExiste_DeveAbrirEIniciarDiagnosticoESalvar()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        ExecucaoOutput? captured = null;
        _outputPort.Setup(o => o.Ok(It.IsAny<ExecucaoOutput>())).Callback<ExecucaoOutput>(o => captured = o);

        await _useCase.Execute(new IniciarDiagnosticoInput(IdOrdemServico));

        _gateway.Verify(g => g.Salvar(
            It.Is<ExecucaoOrdemServico>(e => e.IdOrdemServico == IdOrdemServico && e.Status == StatusExecucaoOrdemServico.EmDiagnostico),
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);

        captured.Should().NotBeNull();
        captured!.IdOrdemServico.Should().Be(IdOrdemServico);
        captured.Status.Should().Be(StatusExecucaoOrdemServico.EmDiagnostico);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoJaExiste_DeveIniciarDiagnosticoEAtualizar()
    {
        var execucaoExistente = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucaoExistente);

        await _useCase.Execute(new IniciarDiagnosticoInput(IdOrdemServico));

        _gateway.Verify(g => g.Atualizar(
            It.Is<ExecucaoOrdemServico>(e => e.Status == StatusExecucaoOrdemServico.EmDiagnostico),
            It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(g => g.Salvar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Once);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoJaEmDiagnostico_DevePropagarDomainExceptionSemAtualizarOuNotificar()
    {
        var execucaoEmDiagnostico = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucaoEmDiagnostico.IniciarDiagnostico();

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucaoEmDiagnostico);

        var acao = async () => await _useCase.Execute(new IniciarDiagnosticoInput(IdOrdemServico));

        await acao.Should().ThrowAsync<DomainException>();
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
    }
}

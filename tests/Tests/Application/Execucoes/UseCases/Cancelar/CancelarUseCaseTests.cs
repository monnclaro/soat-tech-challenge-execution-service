using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.Cancelar;
using Domain.Common.Exceptions;
using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.Cancelar;

public class CancelarUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<ICancelarOutputPort> _outputPort = new();
    private readonly CancelarUseCase _useCase;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public CancelarUseCaseTests()
    {
        _useCase = new CancelarUseCase(_gateway.Object, _outputPort.Object);
    }

    [Fact]
    public async Task Execute_QuandoExecucaoNaoExiste_DeveNotificarNaoEncontrado()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        await _useCase.Execute(new CancelarInput(IdOrdemServico, "Peça indisponível"));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_QuandoEmDiagnostico_DeveCancelarEAtualizar()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        await _useCase.Execute(new CancelarInput(IdOrdemServico, "Veículo não atendível"));

        _gateway.Verify(g => g.Atualizar(
            It.Is<ExecucaoOrdemServico>(e => e.Status == StatusExecucaoOrdemServico.Cancelada && e.MotivoCancelamento == "Veículo não atendível"),
            It.IsAny<CancellationToken>()), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Once);
    }

    [Fact]
    public async Task Execute_QuandoJaFinalizada_DevePropagarDomainExceptionSemAtualizar()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);
        execucao.FinalizarDiagnostico();
        var idServico = execucao.Servicos[0].IdServico;
        execucao.IniciarExecucaoServico(idServico);
        execucao.FinalizarExecucaoServico(idServico);

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var acao = async () => await _useCase.Execute(new CancelarInput(IdOrdemServico, "Motivo qualquer"));

        await acao.Should().ThrowAsync<DomainException>();
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
    }
}

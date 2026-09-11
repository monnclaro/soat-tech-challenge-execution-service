using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.IniciarExecucaoServico;
using Domain.Common.Exceptions;
using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using Domain.Execucoes.Itens.Enums;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.IniciarExecucaoServico;

public class IniciarExecucaoServicoUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<IIniciarExecucaoServicoOutputPort> _outputPort = new();
    private readonly IniciarExecucaoServicoUseCase _useCase;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public IniciarExecucaoServicoUseCaseTests()
    {
        _useCase = new IniciarExecucaoServicoUseCase(_gateway.Object, _outputPort.Object);
    }

    private static ExecucaoOrdemServico CriarComDiagnosticoFinalizado(out Guid idServico)
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);
        execucao.FinalizarDiagnostico();
        idServico = execucao.Servicos[0].IdServico;
        return execucao;
    }

    [Fact]
    public async Task Execute_QuandoExecucaoNaoExiste_DeveNotificarNaoEncontrado()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        await _useCase.Execute(new IniciarExecucaoServicoInput(IdOrdemServico, Guid.NewGuid()));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ComServicoPendente_DeveIniciarExecucaoEAtualizar()
    {
        var execucao = CriarComDiagnosticoFinalizado(out var idServico);
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        ExecucaoOutput? captured = null;
        _outputPort.Setup(o => o.Ok(It.IsAny<ExecucaoOutput>())).Callback<ExecucaoOutput>(o => captured = o);

        await _useCase.Execute(new IniciarExecucaoServicoInput(IdOrdemServico, idServico));

        _gateway.Verify(g => g.Atualizar(
            It.Is<ExecucaoOrdemServico>(e => e.Status == StatusExecucaoOrdemServico.EmExecucao),
            It.IsAny<CancellationToken>()), Times.Once);

        captured.Should().NotBeNull();
        captured!.Servicos.Should().ContainSingle(s => s.IdServico == idServico && s.Status == StatusItemServico.EmExecucao);
    }

    [Fact]
    public async Task Execute_ServicoNaoVinculado_DevePropagarDomainExceptionSemAtualizarOuNotificarOk()
    {
        var execucao = CriarComDiagnosticoFinalizado(out _);
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var acao = async () => await _useCase.Execute(new IniciarExecucaoServicoInput(IdOrdemServico, Guid.NewGuid()));

        await acao.Should().ThrowAsync<DomainException>();
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
    }
}

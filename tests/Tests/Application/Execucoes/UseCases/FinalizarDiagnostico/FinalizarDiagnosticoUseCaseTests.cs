using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.FinalizarDiagnostico;
using Domain.Common.Exceptions;
using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Moq;

namespace Tests.Application.Execucoes.UseCases.FinalizarDiagnostico;

public class FinalizarDiagnosticoUseCaseTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly Mock<IFinalizarDiagnosticoOutputPort> _outputPort = new();
    private readonly FinalizarDiagnosticoUseCase _useCase;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public FinalizarDiagnosticoUseCaseTests()
    {
        _useCase = new FinalizarDiagnosticoUseCase(_gateway.Object, _outputPort.Object);
    }

    private static ExecucaoOrdemServico CriarEmDiagnostico()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        return execucao;
    }

    [Fact]
    public async Task Execute_QuandoExecucaoNaoExiste_DeveNotificarNaoEncontrado()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        await _useCase.Execute(new FinalizarDiagnosticoInput(IdOrdemServico, [], []));

        _outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Execute_ComServicosEProdutos_DeveAdicionarItensFinalizarEAtualizar()
    {
        var execucao = CriarEmDiagnostico();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var idServico = Guid.NewGuid();
        var idProduto = Guid.NewGuid();
        var input = new FinalizarDiagnosticoInput(
            IdOrdemServico,
            [new ItemServicoDiagnosticadoInput(idServico, "Troca de óleo", 100m)],
            [new ItemProdutoDiagnosticadoInput(idProduto, "Filtro de óleo", 50m, 2m)]);

        ExecucaoOutput? captured = null;
        _outputPort.Setup(o => o.Ok(It.IsAny<ExecucaoOutput>())).Callback<ExecucaoOutput>(o => captured = o);

        await _useCase.Execute(input);

        _gateway.Verify(g => g.Atualizar(
            It.Is<ExecucaoOrdemServico>(e => e.Status == StatusExecucaoOrdemServico.DiagnosticoFinalizado),
            It.IsAny<CancellationToken>()), Times.Once);

        captured.Should().NotBeNull();
        captured!.Status.Should().Be(StatusExecucaoOrdemServico.DiagnosticoFinalizado);
        captured.Servicos.Should().ContainSingle(s => s.IdServico == idServico && s.NomeServico == "Troca de óleo" && s.Valor == 100m);
        captured.Produtos.Should().ContainSingle(p => p.IdProduto == idProduto && p.NomeProduto == "Filtro de óleo" && p.Subtotal == 100m);
    }

    [Fact]
    public async Task Execute_SemServicos_DevePropagarDomainExceptionSemAtualizarOuNotificarOk()
    {
        var execucao = CriarEmDiagnostico();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var input = new FinalizarDiagnosticoInput(IdOrdemServico, [], []);

        var acao = async () => await _useCase.Execute(input);

        await acao.Should().ThrowAsync<DomainException>();
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
        _outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Never);
    }
}

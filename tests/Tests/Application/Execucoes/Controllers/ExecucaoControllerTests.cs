using Application.Execucoes.Controllers;
using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarFila;
using Application.Execucoes.UseCases.BuscarPorOrdemServico;
using Application.Execucoes.UseCases.FinalizarDiagnostico;
using Application.Execucoes.UseCases.FinalizarExecucaoServico;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Application.Execucoes.UseCases.IniciarExecucaoServico;
using Domain.Execucoes;
using Domain.Execucoes.Gateways;
using Moq;

namespace Tests.Application.Execucoes.Controllers;

// Controller de aplicação: cada método apenas delega para o UseCase correspondente.
// Testado fim a fim com UseCases reais + gateway mockado, para garantir que a delegação
// (Input correto, ct propagado) realmente acontece — não seria pego por um mock do
// próprio UseCase, já que eles são classes concretas sem interface própria.
public class ExecucaoControllerTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    private ExecucaoController CriarController(
        Mock<IBuscarFilaOutputPort>? buscarFilaOutput = null,
        Mock<IBuscarPorOrdemServicoOutputPort>? buscarPorOrdemServicoOutput = null,
        Mock<IIniciarDiagnosticoOutputPort>? iniciarDiagnosticoOutput = null,
        Mock<IFinalizarDiagnosticoOutputPort>? finalizarDiagnosticoOutput = null,
        Mock<IIniciarExecucaoServicoOutputPort>? iniciarExecucaoServicoOutput = null,
        Mock<IFinalizarExecucaoServicoOutputPort>? finalizarExecucaoServicoOutput = null) => new(
        new BuscarFilaUseCase(_gateway.Object, (buscarFilaOutput ?? new Mock<IBuscarFilaOutputPort>()).Object),
        new BuscarPorOrdemServicoUseCase(_gateway.Object, (buscarPorOrdemServicoOutput ?? new Mock<IBuscarPorOrdemServicoOutputPort>()).Object),
        new IniciarDiagnosticoUseCase(_gateway.Object, (iniciarDiagnosticoOutput ?? new Mock<IIniciarDiagnosticoOutputPort>()).Object),
        new FinalizarDiagnosticoUseCase(_gateway.Object, (finalizarDiagnosticoOutput ?? new Mock<IFinalizarDiagnosticoOutputPort>()).Object),
        new IniciarExecucaoServicoUseCase(_gateway.Object, (iniciarExecucaoServicoOutput ?? new Mock<IIniciarExecucaoServicoOutputPort>()).Object),
        new FinalizarExecucaoServicoUseCase(_gateway.Object, (finalizarExecucaoServicoOutput ?? new Mock<IFinalizarExecucaoServicoOutputPort>()).Object));

    [Fact]
    public async Task BuscarFila_DeveDelegarParaUseCaseENotificarOutputPort()
    {
        var outputPort = new Mock<IBuscarFilaOutputPort>();
        _gateway.Setup(g => g.BuscarFila(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<ExecucaoOrdemServico>)[]);
        var controller = CriarController(buscarFilaOutput: outputPort);

        await controller.BuscarFila(new BuscarFilaInput());

        outputPort.Verify(o => o.Ok(It.IsAny<IReadOnlyList<ExecucaoOutput>>()), Times.Once);
    }

    [Fact]
    public async Task BuscarPorOrdemServico_DeveDelegarParaUseCaseENotificarNaoEncontrado()
    {
        var outputPort = new Mock<IBuscarPorOrdemServicoOutputPort>();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>())).ReturnsAsync((ExecucaoOrdemServico?)null);
        var controller = CriarController(buscarPorOrdemServicoOutput: outputPort);

        await controller.BuscarPorOrdemServico(new BuscarPorOrdemServicoInput(IdOrdemServico));

        outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
    }

    [Fact]
    public async Task IniciarDiagnostico_DeveDelegarParaUseCaseESalvarNovaExecucao()
    {
        var outputPort = new Mock<IIniciarDiagnosticoOutputPort>();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>())).ReturnsAsync((ExecucaoOrdemServico?)null);
        var controller = CriarController(iniciarDiagnosticoOutput: outputPort);

        await controller.IniciarDiagnostico(new IniciarDiagnosticoInput(IdOrdemServico));

        _gateway.Verify(g => g.Salvar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Once);
        outputPort.Verify(o => o.Ok(It.IsAny<ExecucaoOutput>()), Times.Once);
    }

    [Fact]
    public async Task FinalizarDiagnostico_DeveDelegarParaUseCaseENotificarNaoEncontrado()
    {
        var outputPort = new Mock<IFinalizarDiagnosticoOutputPort>();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>())).ReturnsAsync((ExecucaoOrdemServico?)null);
        var controller = CriarController(finalizarDiagnosticoOutput: outputPort);

        await controller.FinalizarDiagnostico(new FinalizarDiagnosticoInput(IdOrdemServico, [], []));

        outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
    }

    [Fact]
    public async Task IniciarExecucaoServico_DeveDelegarParaUseCaseENotificarNaoEncontrado()
    {
        var outputPort = new Mock<IIniciarExecucaoServicoOutputPort>();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>())).ReturnsAsync((ExecucaoOrdemServico?)null);
        var controller = CriarController(iniciarExecucaoServicoOutput: outputPort);

        await controller.IniciarExecucaoServico(new IniciarExecucaoServicoInput(IdOrdemServico, Guid.NewGuid()));

        outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
    }

    [Fact]
    public async Task FinalizarExecucaoServico_DeveDelegarParaUseCaseENotificarNaoEncontrado()
    {
        var outputPort = new Mock<IFinalizarExecucaoServicoOutputPort>();
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>())).ReturnsAsync((ExecucaoOrdemServico?)null);
        var controller = CriarController(finalizarExecucaoServicoOutput: outputPort);

        await controller.FinalizarExecucaoServico(new FinalizarExecucaoServicoInput(IdOrdemServico, Guid.NewGuid()));

        outputPort.Verify(o => o.NaoEncontrado(), Times.Once);
    }
}

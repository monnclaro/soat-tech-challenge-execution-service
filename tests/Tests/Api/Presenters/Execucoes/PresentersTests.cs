using Api.Presenters.Execucoes;
using Application.Execucoes.UseCases;
using Domain.Execucoes.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api.Presenters.Execucoes;

// Presenters são adaptadores triviais (Output Port -> IActionResult), mas ainda assim
// possuem múltiplos membros (Ok/NaoEncontrado) que valem a pena verificar diretamente.
public class PresentersTests
{
    private static readonly ExecucaoOutput Output = new(
        Guid.NewGuid(),
        StatusExecucaoOrdemServico.AguardandoDiagnostico,
        DateTime.UtcNow,
        null,
        null,
        null,
        [],
        []);

    [Fact]
    public void BuscarFilaPresenter_Ok_DeveRetornarOkObjectResultComALista()
    {
        var presenter = new BuscarFilaPresenter();

        presenter.Ok([Output]);

        presenter.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new[] { Output });
    }

    [Fact]
    public void BuscarPorOrdemServicoPresenter_Ok_DeveRetornarOkObjectResultComOOutput()
    {
        var presenter = new BuscarPorOrdemServicoPresenter();

        presenter.Ok(Output);

        presenter.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Output);
    }

    [Fact]
    public void BuscarPorOrdemServicoPresenter_NaoEncontrado_DeveRetornarNotFoundResult()
    {
        var presenter = new BuscarPorOrdemServicoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void IniciarDiagnosticoPresenter_Ok_DeveRetornarOkObjectResultComOOutput()
    {
        var presenter = new IniciarDiagnosticoPresenter();

        presenter.Ok(Output);

        presenter.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Output);
    }

    [Fact]
    public void FinalizarDiagnosticoPresenter_Ok_DeveRetornarOkObjectResultComOOutput()
    {
        var presenter = new FinalizarDiagnosticoPresenter();

        presenter.Ok(Output);

        presenter.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Output);
    }

    [Fact]
    public void FinalizarDiagnosticoPresenter_NaoEncontrado_DeveRetornarNotFoundResult()
    {
        var presenter = new FinalizarDiagnosticoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void IniciarExecucaoServicoPresenter_Ok_DeveRetornarOkObjectResultComOOutput()
    {
        var presenter = new IniciarExecucaoServicoPresenter();

        presenter.Ok(Output);

        presenter.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Output);
    }

    [Fact]
    public void IniciarExecucaoServicoPresenter_NaoEncontrado_DeveRetornarNotFoundResult()
    {
        var presenter = new IniciarExecucaoServicoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void FinalizarExecucaoServicoPresenter_Ok_DeveRetornarOkObjectResultComOOutput()
    {
        var presenter = new FinalizarExecucaoServicoPresenter();

        presenter.Ok(Output);

        presenter.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(Output);
    }

    [Fact]
    public void FinalizarExecucaoServicoPresenter_NaoEncontrado_DeveRetornarNotFoundResult()
    {
        var presenter = new FinalizarExecucaoServicoPresenter();

        presenter.NaoEncontrado();

        presenter.Result.Should().BeOfType<NotFoundResult>();
    }
}

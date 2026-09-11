using Api.Controllers.Execucoes.Requests;
using Api.Presenters.Execucoes;
using Application.Execucoes.Controllers;
using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarFila;
using Application.Execucoes.UseCases.BuscarPorOrdemServico;
using Application.Execucoes.UseCases.FinalizarDiagnostico;
using Application.Execucoes.UseCases.FinalizarExecucaoServico;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Application.Execucoes.UseCases.IniciarExecucaoServico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Execucoes;

// Superfície REST conforme PLANO-FASE-4-MICROSSERVICOS.md, seção 1.3. As transições
// PATCH aqui expostas (diagnóstico/execução) seriam disparadas por mensageria em
// produção (comandos IniciarDiagnostico/IniciarExecucao do OS Service) — a via REST é
// o scaffold desta fase, a substituição por consumers chega num follow-up PR.
[ApiController]
[Route("api/v1")]
[Authorize]
[Produces("application/json")]
public class ExecucoesController : ControllerBase
{
    private readonly ExecucaoController _controller;
    private readonly BuscarFilaPresenter _buscarFilaPresenter;
    private readonly BuscarPorOrdemServicoPresenter _buscarPorOrdemServicoPresenter;
    private readonly IniciarDiagnosticoPresenter _iniciarDiagnosticoPresenter;
    private readonly FinalizarDiagnosticoPresenter _finalizarDiagnosticoPresenter;
    private readonly IniciarExecucaoServicoPresenter _iniciarExecucaoServicoPresenter;
    private readonly FinalizarExecucaoServicoPresenter _finalizarExecucaoServicoPresenter;

    public ExecucoesController(
        ExecucaoController controller,
        BuscarFilaPresenter buscarFilaPresenter,
        BuscarPorOrdemServicoPresenter buscarPorOrdemServicoPresenter,
        IniciarDiagnosticoPresenter iniciarDiagnosticoPresenter,
        FinalizarDiagnosticoPresenter finalizarDiagnosticoPresenter,
        IniciarExecucaoServicoPresenter iniciarExecucaoServicoPresenter,
        FinalizarExecucaoServicoPresenter finalizarExecucaoServicoPresenter)
    {
        _controller = controller;
        _buscarFilaPresenter = buscarFilaPresenter;
        _buscarPorOrdemServicoPresenter = buscarPorOrdemServicoPresenter;
        _iniciarDiagnosticoPresenter = iniciarDiagnosticoPresenter;
        _finalizarDiagnosticoPresenter = finalizarDiagnosticoPresenter;
        _iniciarExecucaoServicoPresenter = iniciarExecucaoServicoPresenter;
        _finalizarExecucaoServicoPresenter = finalizarExecucaoServicoPresenter;
    }

    [HttpGet("fila")]
    [ProducesResponseType(typeof(IReadOnlyList<ExecucaoOutput>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BuscarFila(CancellationToken ct)
    {
        await _controller.BuscarFila(new BuscarFilaInput(), ct);
        return _buscarFilaPresenter.Result!;
    }

    [HttpGet("execucoes/{idOrdemServico:guid}")]
    [ProducesResponseType(typeof(ExecucaoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BuscarPorOrdemServico([FromRoute] Guid idOrdemServico, CancellationToken ct)
    {
        await _controller.BuscarPorOrdemServico(new BuscarPorOrdemServicoInput(idOrdemServico), ct);
        return _buscarPorOrdemServicoPresenter.Result!;
    }

    [HttpPatch("execucoes/{idOrdemServico:guid}/diagnostico/iniciar")]
    [ProducesResponseType(typeof(ExecucaoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IniciarDiagnostico([FromRoute] Guid idOrdemServico, CancellationToken ct)
    {
        await _controller.IniciarDiagnostico(new IniciarDiagnosticoInput(idOrdemServico), ct);
        return _iniciarDiagnosticoPresenter.Result!;
    }

    [HttpPatch("execucoes/{idOrdemServico:guid}/diagnostico/finalizar")]
    [ProducesResponseType(typeof(ExecucaoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizarDiagnostico(
        [FromRoute] Guid idOrdemServico,
        [FromBody] FinalizarDiagnosticoRequest request,
        CancellationToken ct)
    {
        await _controller.FinalizarDiagnostico(new FinalizarDiagnosticoInput(
            idOrdemServico,
            [.. request.Servicos.Select(s => new ItemServicoDiagnosticadoInput(s.IdServico, s.NomeServico, s.Valor))],
            [.. request.Produtos.Select(p => new ItemProdutoDiagnosticadoInput(p.IdProduto, p.NomeProduto, p.ValorUnitario, p.Quantidade))]
        ), ct);

        return _finalizarDiagnosticoPresenter.Result!;
    }

    [HttpPatch("execucoes/{idOrdemServico:guid}/servicos/{idServico:guid}/iniciar-execucao")]
    [ProducesResponseType(typeof(ExecucaoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IniciarExecucaoServico(
        [FromRoute] Guid idOrdemServico,
        [FromRoute] Guid idServico,
        CancellationToken ct)
    {
        await _controller.IniciarExecucaoServico(new IniciarExecucaoServicoInput(idOrdemServico, idServico), ct);
        return _iniciarExecucaoServicoPresenter.Result!;
    }

    [HttpPatch("execucoes/{idOrdemServico:guid}/servicos/{idServico:guid}/finalizar-execucao")]
    [ProducesResponseType(typeof(ExecucaoOutput), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FinalizarExecucaoServico(
        [FromRoute] Guid idOrdemServico,
        [FromRoute] Guid idServico,
        CancellationToken ct)
    {
        await _controller.FinalizarExecucaoServico(new FinalizarExecucaoServicoInput(idOrdemServico, idServico), ct);
        return _finalizarExecucaoServicoPresenter.Result!;
    }
}

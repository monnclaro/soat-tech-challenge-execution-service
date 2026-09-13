using Api.Extensions.Markers;
using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarPorOrdemServico;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Execucoes;

public class BuscarPorOrdemServicoPresenter : IBuscarPorOrdemServicoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }
    public void NaoEncontrado() => Result = new NotFoundResult();
    public void Ok(ExecucaoOutput output) => Result = new OkObjectResult(output);
}

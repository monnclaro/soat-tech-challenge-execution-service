using Api.Extensions.Markers;
using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.BuscarFila;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Execucoes;

public class BuscarFilaPresenter : IBuscarFilaOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }
    public void Ok(IReadOnlyList<ExecucaoOutput> fila) => Result = new OkObjectResult(fila);
}

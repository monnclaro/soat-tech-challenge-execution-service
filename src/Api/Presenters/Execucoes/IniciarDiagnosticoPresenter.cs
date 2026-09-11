using Api.Extensions.Markers;
using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Microsoft.AspNetCore.Mvc;

namespace Api.Presenters.Execucoes;

public class IniciarDiagnosticoPresenter : IIniciarDiagnosticoOutputPort, IPresenter
{
    public IActionResult? Result { get; private set; }
    public void Ok(ExecucaoOutput output) => Result = new OkObjectResult(output);
}

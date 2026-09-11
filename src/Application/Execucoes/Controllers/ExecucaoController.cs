using Application.Execucoes.UseCases.BuscarFila;
using Application.Execucoes.UseCases.BuscarPorOrdemServico;
using Application.Execucoes.UseCases.FinalizarDiagnostico;
using Application.Execucoes.UseCases.FinalizarExecucaoServico;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Application.Execucoes.UseCases.IniciarExecucaoServico;
using SharedKernel.Interfaces;

namespace Application.Execucoes.Controllers;

public class ExecucaoController : IScoped
{
    private readonly BuscarFilaUseCase _buscarFila;
    private readonly BuscarPorOrdemServicoUseCase _buscarPorOrdemServico;
    private readonly IniciarDiagnosticoUseCase _iniciarDiagnostico;
    private readonly FinalizarDiagnosticoUseCase _finalizarDiagnostico;
    private readonly IniciarExecucaoServicoUseCase _iniciarExecucaoServico;
    private readonly FinalizarExecucaoServicoUseCase _finalizarExecucaoServico;

    public ExecucaoController(
        BuscarFilaUseCase buscarFila,
        BuscarPorOrdemServicoUseCase buscarPorOrdemServico,
        IniciarDiagnosticoUseCase iniciarDiagnostico,
        FinalizarDiagnosticoUseCase finalizarDiagnostico,
        IniciarExecucaoServicoUseCase iniciarExecucaoServico,
        FinalizarExecucaoServicoUseCase finalizarExecucaoServico)
    {
        _buscarFila = buscarFila;
        _buscarPorOrdemServico = buscarPorOrdemServico;
        _iniciarDiagnostico = iniciarDiagnostico;
        _finalizarDiagnostico = finalizarDiagnostico;
        _iniciarExecucaoServico = iniciarExecucaoServico;
        _finalizarExecucaoServico = finalizarExecucaoServico;
    }

    public async Task BuscarFila(BuscarFilaInput input, CancellationToken ct = default)
        => await _buscarFila.Execute(input, ct);

    public async Task BuscarPorOrdemServico(BuscarPorOrdemServicoInput input, CancellationToken ct = default)
        => await _buscarPorOrdemServico.Execute(input, ct);

    public async Task IniciarDiagnostico(IniciarDiagnosticoInput input, CancellationToken ct = default)
        => await _iniciarDiagnostico.Execute(input, ct);

    public async Task FinalizarDiagnostico(FinalizarDiagnosticoInput input, CancellationToken ct = default)
        => await _finalizarDiagnostico.Execute(input, ct);

    public async Task IniciarExecucaoServico(IniciarExecucaoServicoInput input, CancellationToken ct = default)
        => await _iniciarExecucaoServico.Execute(input, ct);

    public async Task FinalizarExecucaoServico(FinalizarExecucaoServicoInput input, CancellationToken ct = default)
        => await _finalizarExecucaoServico.Execute(input, ct);
}

using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.Cancelar;

// Caminho de compensação da saga: veículo não atendível durante o diagnóstico, ou
// falha durante a execução (ex.: peça indisponível). Hoje disparado via endpoint
// interno (Admin-only) — o agregado decide sozinho (Cancelar) se isso equivale a
// DiagnosticoFalhou ou ExecucaoFalhou, dependendo da fase em que ocorreu.
public class CancelarUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly ICancelarOutputPort _outputPort;

    public CancelarUseCase(IExecucaoOrdemServicoGateway gateway, ICancelarOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(CancelarInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        execucao.Cancelar(input.Motivo);

        await _gateway.Atualizar(execucao, ct);

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

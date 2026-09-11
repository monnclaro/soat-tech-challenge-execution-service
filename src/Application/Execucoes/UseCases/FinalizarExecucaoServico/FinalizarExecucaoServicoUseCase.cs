using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.FinalizarExecucaoServico;

public class FinalizarExecucaoServicoUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IFinalizarExecucaoServicoOutputPort _outputPort;

    public FinalizarExecucaoServicoUseCase(IExecucaoOrdemServicoGateway gateway, IFinalizarExecucaoServicoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(FinalizarExecucaoServicoInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        execucao.FinalizarExecucaoServico(input.IdServico);

        await _gateway.Atualizar(execucao, ct);

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

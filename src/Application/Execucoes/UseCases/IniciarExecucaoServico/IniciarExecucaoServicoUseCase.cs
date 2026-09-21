using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.IniciarExecucaoServico;

public class IniciarExecucaoServicoUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IIniciarExecucaoServicoOutputPort _outputPort;

    public IniciarExecucaoServicoUseCase(IExecucaoOrdemServicoGateway gateway, IIniciarExecucaoServicoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(IniciarExecucaoServicoInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        execucao.IniciarExecucaoServico(input.IdServico);

        await _gateway.Atualizar(execucao, ct);

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

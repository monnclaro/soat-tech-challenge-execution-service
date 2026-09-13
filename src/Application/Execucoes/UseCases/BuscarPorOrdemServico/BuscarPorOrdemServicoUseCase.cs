using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.BuscarPorOrdemServico;

public class BuscarPorOrdemServicoUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IBuscarPorOrdemServicoOutputPort _outputPort;

    public BuscarPorOrdemServicoUseCase(IExecucaoOrdemServicoGateway gateway, IBuscarPorOrdemServicoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(BuscarPorOrdemServicoInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

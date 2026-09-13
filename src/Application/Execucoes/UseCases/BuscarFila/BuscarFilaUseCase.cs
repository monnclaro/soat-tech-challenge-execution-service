using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.BuscarFila;

// "Fila de execução" consultada pelos mecânicos: toda execução que ainda não chegou
// a um estado terminal (Finalizada/Cancelada).
public class BuscarFilaUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IBuscarFilaOutputPort _outputPort;

    public BuscarFilaUseCase(IExecucaoOrdemServicoGateway gateway, IBuscarFilaOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(BuscarFilaInput input, CancellationToken ct = default)
    {
        var fila = await _gateway.BuscarFila(ct);

        _outputPort.Ok([.. fila.Select(ExecucaoOutputMapper.Map)]);
    }
}

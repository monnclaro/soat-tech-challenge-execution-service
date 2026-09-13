namespace Domain.Execucoes.Gateways;

public interface IExecucaoOrdemServicoGateway
{
    Task<ExecucaoOrdemServico?> BuscarPorIdOrdemServico(Guid idOrdemServico, CancellationToken ct = default);

    // "Fila de execução": registros que ainda não chegaram a um estado terminal
    // (Finalizada/Cancelada) — é o que os mecânicos acompanham.
    Task<IReadOnlyList<ExecucaoOrdemServico>> BuscarFila(CancellationToken ct = default);

    Task Salvar(ExecucaoOrdemServico execucao, CancellationToken ct = default);

    Task Atualizar(ExecucaoOrdemServico execucao, CancellationToken ct = default);
}

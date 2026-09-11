using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using Infrastructure.Database;
using Infrastructure.Database.Mappers;
using Infrastructure.DomainEvents;
using MongoDB.Driver;

namespace Infrastructure.Gateways.Execucoes;

public class ExecucaoOrdemServicoGateway : IExecucaoOrdemServicoGateway
{
    private readonly MongoContext _context;
    private readonly IDomainEventsDispatcher _dispatcher;

    public ExecucaoOrdemServicoGateway(MongoContext context, IDomainEventsDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<ExecucaoOrdemServico?> BuscarPorIdOrdemServico(Guid idOrdemServico, CancellationToken ct = default)
    {
        var document = await _context.Execucoes
            .Find(d => d.IdOrdemServico == idOrdemServico)
            .FirstOrDefaultAsync(ct);

        return document is null ? null : ExecucaoOrdemServicoMapper.ToDomain(document);
    }

    public async Task<IReadOnlyList<ExecucaoOrdemServico>> BuscarFila(CancellationToken ct = default)
    {
        var documentos = await _context.Execucoes
            .Find(d => d.Status != StatusExecucaoOrdemServico.Finalizada && d.Status != StatusExecucaoOrdemServico.Cancelada)
            .SortBy(d => d.DataCriacao)
            .ToListAsync(ct);

        return [.. documentos.Select(ExecucaoOrdemServicoMapper.ToDomain)];
    }

    public async Task Salvar(ExecucaoOrdemServico execucao, CancellationToken ct = default)
    {
        await _context.Execucoes.InsertOneAsync(ExecucaoOrdemServicoMapper.ToDocument(execucao), cancellationToken: ct);

        await DispatchDomainEventsAsync(execucao, ct);
    }

    public async Task Atualizar(ExecucaoOrdemServico execucao, CancellationToken ct = default)
    {
        await _context.Execucoes.ReplaceOneAsync(
            d => d.IdOrdemServico == execucao.IdOrdemServico,
            ExecucaoOrdemServicoMapper.ToDocument(execucao),
            new ReplaceOptions { IsUpsert = true },
            ct);

        await DispatchDomainEventsAsync(execucao, ct);
    }

    private async Task DispatchDomainEventsAsync(ExecucaoOrdemServico execucao, CancellationToken ct)
    {
        var domainEvents = execucao.DomainEvents;
        execucao.ClearDomainEvents();

        await _dispatcher.DispatchAsync(domainEvents, ct);
    }
}

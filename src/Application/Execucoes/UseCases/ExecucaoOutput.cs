using Domain.Execucoes.Enums;

namespace Application.Execucoes.UseCases;

public record ExecucaoOutput(
    Guid IdOrdemServico,
    StatusExecucaoOrdemServico Status,
    DateTime DataCriacao,
    DateTime? DataFinalizacaoDiagnostico,
    DateTime? DataInicioExecucao,
    DateTime? DataFinalizacao,
    IReadOnlyList<ItemServicoOutput> Servicos,
    IReadOnlyList<ItemProdutoOutput> Produtos);

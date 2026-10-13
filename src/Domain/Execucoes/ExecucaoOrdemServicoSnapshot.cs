using Domain.Execucoes.Enums;
using Domain.Execucoes.Itens;

namespace Domain.Execucoes;

// Agrupa os campos usados só por ExecucaoOrdemServico.Reidratar — existe para não
// estourar o limite de parâmetros por método (regra SonarCloud S107), não porque
// tenha significado de negócio próprio fora da reconstrução a partir do MongoDB.
public sealed record ExecucaoOrdemServicoSnapshot(
    Guid IdOrdemServico,
    StatusExecucaoOrdemServico Status,
    DateTime DataCriacao,
    DateTime? DataFinalizacaoDiagnostico,
    DateTime? DataInicioExecucao,
    DateTime? DataFinalizacao,
    string? MotivoCancelamento,
    List<ItemServico> Servicos,
    List<ItemProduto> Produtos);

using Domain.Execucoes;

namespace Application.Execucoes.UseCases;

internal static class ExecucaoOutputMapper
{
    public static ExecucaoOutput Map(ExecucaoOrdemServico execucao) => new(
        execucao.IdOrdemServico,
        execucao.Status,
        execucao.DataCriacao,
        execucao.DataFinalizacaoDiagnostico,
        execucao.DataInicioExecucao,
        execucao.DataFinalizacao,
        [.. execucao.Servicos.Select(s => new ItemServicoOutput(
            s.Id, s.IdServico, s.NomeServico, s.Valor, s.Status, s.DataInicioExecucao, s.DataFinalizacaoExecucao))],
        [.. execucao.Produtos.Select(p => new ItemProdutoOutput(
            p.Id, p.IdProduto, p.NomeProduto, p.ValorUnitario, p.Quantidade, p.Subtotal))]);
}

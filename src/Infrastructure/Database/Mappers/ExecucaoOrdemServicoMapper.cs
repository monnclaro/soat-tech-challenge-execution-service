using Domain.Execucoes;
using Domain.Execucoes.Itens;
using Infrastructure.Database.Documents;

namespace Infrastructure.Database.Mappers;

internal static class ExecucaoOrdemServicoMapper
{
    public static ExecucaoOrdemServicoDocument ToDocument(ExecucaoOrdemServico entity) => new()
    {
        IdOrdemServico = entity.IdOrdemServico,
        Status = entity.Status,
        DataCriacao = entity.DataCriacao,
        DataFinalizacaoDiagnostico = entity.DataFinalizacaoDiagnostico,
        DataInicioExecucao = entity.DataInicioExecucao,
        DataFinalizacao = entity.DataFinalizacao,
        Servicos = [.. entity.Servicos.Select(s => new ItemServicoDocument
        {
            Id = s.Id,
            IdServico = s.IdServico,
            NomeServico = s.NomeServico,
            Valor = s.Valor,
            Status = s.Status,
            DataInicioExecucao = s.DataInicioExecucao,
            DataFinalizacaoExecucao = s.DataFinalizacaoExecucao,
        })],
        Produtos = [.. entity.Produtos.Select(p => new ItemProdutoDocument
        {
            Id = p.Id,
            IdProduto = p.IdProduto,
            NomeProduto = p.NomeProduto,
            ValorUnitario = p.ValorUnitario,
            Quantidade = p.Quantidade,
        })],
    };

    public static ExecucaoOrdemServico ToDomain(ExecucaoOrdemServicoDocument document) =>
        ExecucaoOrdemServico.Reidratar(
            document.IdOrdemServico,
            document.Status,
            document.DataCriacao,
            document.DataFinalizacaoDiagnostico,
            document.DataInicioExecucao,
            document.DataFinalizacao,
            [.. document.Servicos.Select(s => ItemServico.Reidratar(
                s.Id, s.IdServico, s.NomeServico, s.Valor, s.Status, s.DataInicioExecucao, s.DataFinalizacaoExecucao))],
            [.. document.Produtos.Select(p => ItemProduto.Reidratar(
                p.Id, p.IdProduto, p.NomeProduto, p.ValorUnitario, p.Quantidade))]);
}

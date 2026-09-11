using Domain.Common;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Execucoes.Itens;

// Snapshot de produto identificado no diagnóstico — mesmo shape de
// soat-tech-challenge/src/Domain/OrdensServico/Produtos/OrdemServicoProduto.cs.
// Sem máquina de estados própria: produtos não têm execução, só consumo.
public class ItemProduto : Entity
{
    public Guid Id { get; private set; }
    public Guid IdProduto { get; private set; }
    public string NomeProduto { get; private set; } = null!;
    public decimal ValorUnitario { get; private set; }
    public decimal Quantidade { get; private set; }

    public decimal Subtotal => ValorUnitario * Quantidade;

    private ItemProduto() { }

    public ItemProduto(Guid idProduto, string nomeProduto, decimal valorUnitario, decimal quantidade)
    {
        if (string.IsNullOrWhiteSpace(nomeProduto))
        {
            throw new DomainException("O nome do produto diagnosticado é obrigatório.");
        }

        if (valorUnitario < 0)
        {
            throw new DomainException("O valor unitário do produto diagnosticado não pode ser negativo.");
        }

        if (quantidade <= 0)
        {
            throw new DomainException("A quantidade do produto diagnosticado deve ser maior que zero.");
        }

        Id = Guid.NewGuid();
        IdProduto = idProduto;
        NomeProduto = nomeProduto;
        ValorUnitario = valorUnitario;
        Quantidade = quantidade;
    }

    // Usado exclusivamente pelo mapper de persistência (Infrastructure) para reconstruir
    // o item a partir do documento MongoDB, sem reexecutar as regras de criação.
    public static ItemProduto Reidratar(Guid id, Guid idProduto, string nomeProduto, decimal valorUnitario, decimal quantidade) =>
        new()
        {
            Id = id,
            IdProduto = idProduto,
            NomeProduto = nomeProduto,
            ValorUnitario = valorUnitario,
            Quantidade = quantidade,
        };
}

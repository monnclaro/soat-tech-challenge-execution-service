namespace Application.Execucoes.UseCases;

public record ItemProdutoOutput(
    Guid Id,
    Guid IdProduto,
    string NomeProduto,
    decimal ValorUnitario,
    decimal Quantidade,
    decimal Subtotal);

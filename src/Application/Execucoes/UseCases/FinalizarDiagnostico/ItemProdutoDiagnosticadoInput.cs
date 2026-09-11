namespace Application.Execucoes.UseCases.FinalizarDiagnostico;

public record ItemProdutoDiagnosticadoInput(Guid IdProduto, string NomeProduto, decimal ValorUnitario, decimal Quantidade);

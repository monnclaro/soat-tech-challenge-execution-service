namespace Application.Execucoes.UseCases.FinalizarDiagnostico;

public record FinalizarDiagnosticoInput(
    Guid IdOrdemServico,
    IReadOnlyList<ItemServicoDiagnosticadoInput> Servicos,
    IReadOnlyList<ItemProdutoDiagnosticadoInput> Produtos);

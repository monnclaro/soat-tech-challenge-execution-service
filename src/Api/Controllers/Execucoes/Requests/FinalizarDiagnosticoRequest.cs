using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Api.Controllers.Execucoes.Requests;

// DTOs de request puros (só usados pelo ExecucoesController, excluído da cobertura),
// sem lógica própria — excluídos da cobertura.
[ExcludeFromCodeCoverage]
public record ItemServicoDiagnosticadoRequest(
    [property: JsonPropertyName("idServico")] Guid IdServico,
    [property: JsonPropertyName("nomeServico")] string NomeServico,
    [property: JsonPropertyName("valor")] decimal Valor);

[ExcludeFromCodeCoverage]
public record ItemProdutoDiagnosticadoRequest(
    [property: JsonPropertyName("idProduto")] Guid IdProduto,
    [property: JsonPropertyName("nomeProduto")] string NomeProduto,
    [property: JsonPropertyName("valorUnitario")] decimal ValorUnitario,
    [property: JsonPropertyName("quantidade")] decimal Quantidade);

[ExcludeFromCodeCoverage]
public record FinalizarDiagnosticoRequest(
    [property: JsonPropertyName("servicos")] IReadOnlyList<ItemServicoDiagnosticadoRequest> Servicos,
    [property: JsonPropertyName("produtos")] IReadOnlyList<ItemProdutoDiagnosticadoRequest> Produtos);

using System.Text.Json.Serialization;

namespace Api.Controllers.Execucoes.Requests;

public record ItemServicoDiagnosticadoRequest(
    [property: JsonPropertyName("idServico")] Guid IdServico,
    [property: JsonPropertyName("nomeServico")] string NomeServico,
    [property: JsonPropertyName("valor")] decimal Valor);

public record ItemProdutoDiagnosticadoRequest(
    [property: JsonPropertyName("idProduto")] Guid IdProduto,
    [property: JsonPropertyName("nomeProduto")] string NomeProduto,
    [property: JsonPropertyName("valorUnitario")] decimal ValorUnitario,
    [property: JsonPropertyName("quantidade")] decimal Quantidade);

public record FinalizarDiagnosticoRequest(
    [property: JsonPropertyName("servicos")] IReadOnlyList<ItemServicoDiagnosticadoRequest> Servicos,
    [property: JsonPropertyName("produtos")] IReadOnlyList<ItemProdutoDiagnosticadoRequest> Produtos);

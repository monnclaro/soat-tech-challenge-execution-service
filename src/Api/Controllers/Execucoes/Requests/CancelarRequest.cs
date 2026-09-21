using System.Text.Json.Serialization;

namespace Api.Controllers.Execucoes.Requests;

public record CancelarRequest(
    [property: JsonPropertyName("motivo")] string Motivo);

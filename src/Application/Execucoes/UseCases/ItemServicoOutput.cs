using Domain.Execucoes.Itens.Enums;

namespace Application.Execucoes.UseCases;

public record ItemServicoOutput(
    Guid Id,
    Guid IdServico,
    string NomeServico,
    decimal Valor,
    StatusItemServico Status,
    DateTime? DataInicioExecucao,
    DateTime? DataFinalizacaoExecucao);

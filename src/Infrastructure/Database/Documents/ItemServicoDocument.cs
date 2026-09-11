using Domain.Execucoes.Itens.Enums;

namespace Infrastructure.Database.Documents;

// Documento embutido (array Servicos[] do documento ExecucaoOrdemServicoDocument) — não
// tem coleção própria. Espelha 1:1 os campos de Domain.Execucoes.Itens.ItemServico;
// mantido separado do agregado de domínio para não vazar preocupações de persistência
// (atributos Bson) para dentro do Domain.
public class ItemServicoDocument
{
    public Guid Id { get; set; }
    public Guid IdServico { get; set; }
    public string NomeServico { get; set; } = null!;
    public decimal Valor { get; set; }
    public StatusItemServico Status { get; set; }
    public DateTime? DataInicioExecucao { get; set; }
    public DateTime? DataFinalizacaoExecucao { get; set; }
}

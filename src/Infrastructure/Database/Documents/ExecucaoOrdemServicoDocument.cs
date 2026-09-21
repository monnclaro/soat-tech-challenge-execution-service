using Domain.Execucoes.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace Infrastructure.Database.Documents;

// Um documento por OS na coleção "execucoes" — chave é o próprio IdOrdemServico
// (este serviço nunca gera esse id, só recebe do OS Service), com Servicos[]/Produtos[]
// embutidos: exatamente o desenho descrito na seção 1.3 do plano de migração.
[BsonIgnoreExtraElements]
public class ExecucaoOrdemServicoDocument
{
    [BsonId]
    public Guid IdOrdemServico { get; set; }

    public StatusExecucaoOrdemServico Status { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime? DataFinalizacaoDiagnostico { get; set; }
    public DateTime? DataInicioExecucao { get; set; }
    public DateTime? DataFinalizacao { get; set; }
    public string? MotivoCancelamento { get; set; }

    public List<ItemServicoDocument> Servicos { get; set; } = [];
    public List<ItemProdutoDocument> Produtos { get; set; } = [];
}

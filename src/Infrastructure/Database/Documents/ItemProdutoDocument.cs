namespace Infrastructure.Database.Documents;

// Documento embutido (array Produtos[] do documento ExecucaoOrdemServicoDocument).
public class ItemProdutoDocument
{
    public Guid Id { get; set; }
    public Guid IdProduto { get; set; }
    public string NomeProduto { get; set; } = null!;
    public decimal ValorUnitario { get; set; }
    public decimal Quantidade { get; set; }
}

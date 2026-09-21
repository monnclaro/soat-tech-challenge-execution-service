using Domain.Common;
using Domain.Execucoes.Itens.Enums;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Execucoes.Itens;

// Porta, quase inalterada, a máquina de estados de
// soat-tech-challenge/src/Domain/OrdensServico/Servicos/OrdemServicoServico.cs
// (IniciarExecucao/FinalizarExecucao) — no monolito ela vivia dentro de OrdemServico;
// na Fase 4 ela passa a viver aqui, dentro de ExecucaoOrdemServico.
public class ItemServico : Entity
{
    public Guid Id { get; private set; }
    public Guid IdServico { get; private set; }
    public string NomeServico { get; private set; } = null!;
    public decimal Valor { get; private set; }
    public StatusItemServico Status { get; private set; }
    public DateTime? DataInicioExecucao { get; private set; }
    public DateTime? DataFinalizacaoExecucao { get; private set; }

    private ItemServico() { }

    public ItemServico(Guid idServico, string nomeServico, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(nomeServico))
        {
            throw new DomainException("O nome do serviço diagnosticado é obrigatório.");
        }

        if (valor < 0)
        {
            throw new DomainException("O valor do serviço diagnosticado não pode ser negativo.");
        }

        Id = Guid.NewGuid();
        IdServico = idServico;
        NomeServico = nomeServico;
        Valor = valor;
        Status = StatusItemServico.AguardandoExecucao;
    }

    public void IniciarExecucao()
    {
        if (Status is StatusItemServico.ExecucaoFinalizada)
        {
            throw new DomainException("O serviço já se encontra finalizado.");
        }

        Status = StatusItemServico.EmExecucao;
        DataInicioExecucao = DateTime.UtcNow;
    }

    public void FinalizarExecucao()
    {
        if (Status is StatusItemServico.ExecucaoFinalizada)
        {
            throw new DomainException("O serviço já se encontra finalizado.");
        }

        Status = StatusItemServico.ExecucaoFinalizada;
        DataFinalizacaoExecucao = DateTime.UtcNow;
    }

    // Usado exclusivamente pelo mapper de persistência (Infrastructure) para reconstruir
    // o item a partir do documento MongoDB, sem reexecutar as regras de criação.
    public static ItemServico Reidratar(
        Guid id,
        Guid idServico,
        string nomeServico,
        decimal valor,
        StatusItemServico status,
        DateTime? dataInicioExecucao,
        DateTime? dataFinalizacaoExecucao) =>
        new()
        {
            Id = id,
            IdServico = idServico,
            NomeServico = nomeServico,
            Valor = valor,
            Status = status,
            DataInicioExecucao = dataInicioExecucao,
            DataFinalizacaoExecucao = dataFinalizacaoExecucao,
        };
}

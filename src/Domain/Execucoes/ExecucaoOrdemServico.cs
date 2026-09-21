using Domain.Common;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Eventos;
using Domain.Execucoes.Itens;
using Domain.Execucoes.Itens.Enums;
using DomainException = Domain.Common.Exceptions.DomainException;

namespace Domain.Execucoes;

// Agregado raiz deste serviço. Um documento por Ordem de Serviço (dona do id é o
// OS Service — este serviço nunca gera IdOrdemServico, apenas recebe).
//
// Reúne, adaptadas ao novo desenho de agregado, duas responsabilidades que viviam
// separadas no monolito de origem (soat-tech-challenge):
//   - edição do diagnóstico (InserirServicos/InserirProdutos/RemoverServico/RemoverProduto
//     de src/Domain/OrdensServico/OrdemServico.cs);
//   - máquina de estados de execução por item (IniciarExecucao/FinalizarExecucao de
//     src/Domain/OrdensServico/Servicos/OrdemServicoServico.cs, hoje em Itens/ItemServico.cs).
public class ExecucaoOrdemServico : Entity
{
    public Guid IdOrdemServico { get; private set; }
    public StatusExecucaoOrdemServico Status { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataFinalizacaoDiagnostico { get; private set; }
    public DateTime? DataInicioExecucao { get; private set; }
    public DateTime? DataFinalizacao { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    private readonly List<ItemServico> _servicos = [];
    private readonly List<ItemProduto> _produtos = [];

    public IReadOnlyList<ItemServico> Servicos => _servicos;
    public IReadOnlyList<ItemProduto> Produtos => _produtos;

    private ExecucaoOrdemServico() { }

    // Ponto de entrada da fila de execução para uma OS. Corresponde ao comando
    // IniciarDiagnostico publicado pelo OS Service quando uma OS é criada — hoje
    // exposto via REST (PATCH .../diagnostico/iniciar), consumido por mensageria em
    // um follow-up PR.
    public static ExecucaoOrdemServico Abrir(Guid idOrdemServico)
    {
        if (idOrdemServico == Guid.Empty)
        {
            throw new DomainException("O id da ordem de serviço é obrigatório.");
        }

        return new ExecucaoOrdemServico
        {
            IdOrdemServico = idOrdemServico,
            Status = StatusExecucaoOrdemServico.AguardandoDiagnostico,
            DataCriacao = DateTime.UtcNow,
        };
    }

    public void IniciarDiagnostico()
    {
        if (Status is not StatusExecucaoOrdemServico.AguardandoDiagnostico)
        {
            throw new DomainException("O diagnóstico só pode ser iniciado enquanto a execução aguarda diagnóstico.");
        }

        Status = StatusExecucaoOrdemServico.EmDiagnostico;
    }

    public void AdicionarServicoDiagnosticado(Guid idServico, string nomeServico, decimal valor)
    {
        GarantirEmDiagnostico();

        _servicos.Add(new ItemServico(idServico, nomeServico, valor));
    }

    public void AdicionarProdutoDiagnosticado(Guid idProduto, string nomeProduto, decimal valorUnitario, decimal quantidade)
    {
        GarantirEmDiagnostico();

        _produtos.Add(new ItemProduto(idProduto, nomeProduto, valorUnitario, quantidade));
    }

    public void RemoverServicoDiagnosticado(Guid idItemServico)
    {
        GarantirEmDiagnostico();

        var item = _servicos.FirstOrDefault(s => s.Id == idItemServico)
            ?? throw new DomainException("O serviço informado não se encontra vinculado a esta execução.");

        _servicos.Remove(item);
    }

    public void RemoverProdutoDiagnosticado(Guid idItemProduto)
    {
        GarantirEmDiagnostico();

        var item = _produtos.FirstOrDefault(p => p.Id == idItemProduto)
            ?? throw new DomainException("O produto informado não se encontra vinculado a esta execução.");

        _produtos.Remove(item);
    }

    public void FinalizarDiagnostico()
    {
        if (Status is not StatusExecucaoOrdemServico.EmDiagnostico)
        {
            throw new DomainException("O diagnóstico só pode ser finalizado enquanto estiver em andamento.");
        }

        if (_servicos.Count == 0)
        {
            throw new DomainException("Não é possível finalizar o diagnóstico sem ao menos um serviço identificado.");
        }

        Status = StatusExecucaoOrdemServico.DiagnosticoFinalizado;
        DataFinalizacaoDiagnostico = DateTime.UtcNow;

        Raise(new DiagnosticoFinalizadoDomainEvent(IdOrdemServico, [.. _servicos], [.. _produtos]));
    }

    // Move a execução para EmExecucao na primeira chamada (não existe mais um passo
    // separado de "aprovação de orçamento" aqui — isso é responsabilidade da saga no
    // OS Service/Billing Service; ao chegar aqui, o pagamento já foi aprovado).
    public void IniciarExecucaoServico(Guid idServico)
    {
        if (Status is not (StatusExecucaoOrdemServico.DiagnosticoFinalizado or StatusExecucaoOrdemServico.EmExecucao))
        {
            throw new DomainException("Só é possível iniciar a execução de um serviço após o diagnóstico ser finalizado.");
        }

        var item = _servicos.FirstOrDefault(s => s.IdServico == idServico)
            ?? throw new DomainException("O serviço informado não se encontra vinculado a esta execução.");

        item.IniciarExecucao();

        if (Status is StatusExecucaoOrdemServico.DiagnosticoFinalizado)
        {
            Status = StatusExecucaoOrdemServico.EmExecucao;
            DataInicioExecucao = DateTime.UtcNow;
        }
    }

    public void FinalizarExecucaoServico(Guid idServico)
    {
        if (Status is not StatusExecucaoOrdemServico.EmExecucao)
        {
            throw new DomainException("Só é possível finalizar a execução de um serviço enquanto a execução estiver em andamento.");
        }

        var item = _servicos.FirstOrDefault(s => s.IdServico == idServico)
            ?? throw new DomainException("O serviço informado não se encontra vinculado a esta execução.");

        item.FinalizarExecucao();

        if (_servicos.All(s => s.Status is StatusItemServico.ExecucaoFinalizada))
        {
            Status = StatusExecucaoOrdemServico.Finalizada;
            DataFinalizacao = DateTime.UtcNow;

            Raise(new ExecucaoFinalizadaDomainEvent(IdOrdemServico, [.. _produtos]));
        }
    }

    // Caminho de compensação da saga: veículo não atendível (durante o diagnóstico) ou
    // falha na execução (ex.: peça indisponível). Publica DiagnosticoFalhou ou
    // ExecucaoFalhou (via PublicarExecucaoCanceladaHandler) dependendo da fase em que
    // o cancelamento ocorreu, para o OS Service saber o motivo e compensar a saga.
    public void Cancelar(string motivo)
    {
        if (Status is StatusExecucaoOrdemServico.Finalizada or StatusExecucaoOrdemServico.Cancelada)
        {
            throw new DomainException("Não é possível cancelar uma execução já finalizada ou cancelada.");
        }

        var duranteDiagnostico = Status is StatusExecucaoOrdemServico.AguardandoDiagnostico or StatusExecucaoOrdemServico.EmDiagnostico;

        Status = StatusExecucaoOrdemServico.Cancelada;
        MotivoCancelamento = motivo;
        DataFinalizacao = DateTime.UtcNow;

        Raise(new ExecucaoCanceladaDomainEvent(IdOrdemServico, motivo, duranteDiagnostico));
    }

    private void GarantirEmDiagnostico()
    {
        if (Status is not StatusExecucaoOrdemServico.EmDiagnostico)
        {
            throw new DomainException("Só é possível alterar os itens diagnosticados enquanto o diagnóstico estiver em andamento.");
        }
    }

    // Usado exclusivamente pelo mapper de persistência (Infrastructure) para reconstruir
    // o agregado a partir do documento MongoDB, sem reexecutar as regras de criação.
    public static ExecucaoOrdemServico Reidratar(
        Guid idOrdemServico,
        StatusExecucaoOrdemServico status,
        DateTime dataCriacao,
        DateTime? dataFinalizacaoDiagnostico,
        DateTime? dataInicioExecucao,
        DateTime? dataFinalizacao,
        string? motivoCancelamento,
        List<ItemServico> servicos,
        List<ItemProduto> produtos)
    {
        var execucao = new ExecucaoOrdemServico
        {
            IdOrdemServico = idOrdemServico,
            Status = status,
            DataCriacao = dataCriacao,
            DataFinalizacaoDiagnostico = dataFinalizacaoDiagnostico,
            DataInicioExecucao = dataInicioExecucao,
            DataFinalizacao = dataFinalizacao,
            MotivoCancelamento = motivoCancelamento,
        };

        execucao._servicos.AddRange(servicos);
        execucao._produtos.AddRange(produtos);

        return execucao;
    }
}

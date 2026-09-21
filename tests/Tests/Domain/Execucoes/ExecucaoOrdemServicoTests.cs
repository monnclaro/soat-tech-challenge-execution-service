using Domain.Common.Exceptions;
using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Eventos;
using Domain.Execucoes.Itens.Enums;
using FluentAssertions;

namespace Tests.Domain.Execucoes;

// Cobre o agregado ExecucaoOrdemServico: a edição de diagnóstico portada de
// OrdemServico (InserirServicos/InserirProdutos/RemoverServico/RemoverProduto) e a
// orquestração da execução por item (que delega para ItemServico).
public class ExecucaoOrdemServicoTests
{
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    private static ExecucaoOrdemServico CriarEmDiagnostico()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        return execucao;
    }

    private static ExecucaoOrdemServico CriarComDiagnosticoFinalizado(int quantidadeServicos = 1)
    {
        var execucao = CriarEmDiagnostico();

        for (var i = 0; i < quantidadeServicos; i++)
        {
            execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), $"Serviço {i}", 100m);
        }

        execucao.FinalizarDiagnostico();
        return execucao;
    }

    [Fact]
    public void Abrir_ComIdValido_DeveCriarComStatusAguardandoDiagnostico()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);

        execucao.IdOrdemServico.Should().Be(IdOrdemServico);
        execucao.Status.Should().Be(StatusExecucaoOrdemServico.AguardandoDiagnostico);
        execucao.Servicos.Should().BeEmpty();
        execucao.Produtos.Should().BeEmpty();
    }

    [Fact]
    public void Abrir_ComIdVazio_DeveLancarDomainException()
    {
        var acao = () => ExecucaoOrdemServico.Abrir(Guid.Empty);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void IniciarDiagnostico_QuandoAguardandoDiagnostico_DeveTransicionarParaEmDiagnostico()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);

        execucao.IniciarDiagnostico();

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.EmDiagnostico);
    }

    [Fact]
    public void IniciarDiagnostico_QuandoJaEmDiagnostico_DeveLancarDomainException()
    {
        var execucao = CriarEmDiagnostico();

        var acao = execucao.IniciarDiagnostico;

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdicionarServicoDiagnosticado_ForaDeDiagnostico_DeveLancarDomainException()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);

        var acao = () => execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdicionarServicoDiagnosticado_EmDiagnostico_DeveAdicionarItem()
    {
        var execucao = CriarEmDiagnostico();

        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);

        execucao.Servicos.Should().HaveCount(1);
        execucao.Servicos[0].Status.Should().Be(StatusItemServico.AguardandoExecucao);
    }

    [Fact]
    public void AdicionarProdutoDiagnosticado_EmDiagnostico_DeveAdicionarItem()
    {
        var execucao = CriarEmDiagnostico();

        execucao.AdicionarProdutoDiagnosticado(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);

        execucao.Produtos.Should().HaveCount(1);
        execucao.Produtos[0].Subtotal.Should().Be(100m);
    }

    [Fact]
    public void RemoverServicoDiagnosticado_ItemNaoVinculado_DeveLancarDomainException()
    {
        var execucao = CriarEmDiagnostico();

        var acao = () => execucao.RemoverServicoDiagnosticado(Guid.NewGuid());

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoverServicoDiagnosticado_ItemVinculado_DeveRemover()
    {
        var execucao = CriarEmDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);
        var idItem = execucao.Servicos[0].Id;

        execucao.RemoverServicoDiagnosticado(idItem);

        execucao.Servicos.Should().BeEmpty();
    }

    [Fact]
    public void RemoverProdutoDiagnosticado_ItemVinculado_DeveRemover()
    {
        var execucao = CriarEmDiagnostico();
        execucao.AdicionarProdutoDiagnosticado(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);
        var idItem = execucao.Produtos[0].Id;

        execucao.RemoverProdutoDiagnosticado(idItem);

        execucao.Produtos.Should().BeEmpty();
    }

    [Fact]
    public void FinalizarDiagnostico_SemServicos_DeveLancarDomainException()
    {
        var execucao = CriarEmDiagnostico();

        var acao = execucao.FinalizarDiagnostico;

        acao.Should().Throw<DomainException>()
            .WithMessage("*ao menos um serviço*");
    }

    [Fact]
    public void FinalizarDiagnostico_ForaDeDiagnostico_DeveLancarDomainException()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);

        var acao = execucao.FinalizarDiagnostico;

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void FinalizarDiagnostico_ComServicos_DeveTransicionarERaiseDomainEvent()
    {
        var execucao = CriarEmDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);

        execucao.FinalizarDiagnostico();

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.DiagnosticoFinalizado);
        execucao.DataFinalizacaoDiagnostico.Should().NotBeNull();
        execucao.DomainEvents.Should().ContainSingle(e => e is DiagnosticoFinalizadoDomainEvent);
    }

    [Fact]
    public void IniciarExecucaoServico_AntesDoDiagnosticoFinalizado_DeveLancarDomainException()
    {
        var execucao = CriarEmDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);
        var idServico = execucao.Servicos[0].IdServico;

        var acao = () => execucao.IniciarExecucaoServico(idServico);

        acao.Should().Throw<DomainException>()
            .WithMessage("*diagnóstico ser finalizado*");
    }

    [Fact]
    public void IniciarExecucaoServico_ServicoNaoVinculado_DeveLancarDomainException()
    {
        var execucao = CriarComDiagnosticoFinalizado();

        var acao = () => execucao.IniciarExecucaoServico(Guid.NewGuid());

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void IniciarExecucaoServico_PrimeiraChamada_DeveTransicionarAgregadoParaEmExecucao()
    {
        var execucao = CriarComDiagnosticoFinalizado();
        var idServico = execucao.Servicos[0].IdServico;

        execucao.IniciarExecucaoServico(idServico);

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.EmExecucao);
        execucao.DataInicioExecucao.Should().NotBeNull();
        execucao.Servicos[0].Status.Should().Be(StatusItemServico.EmExecucao);
    }

    [Fact]
    public void FinalizarExecucaoServico_ForaDeExecucao_DeveLancarDomainException()
    {
        var execucao = CriarComDiagnosticoFinalizado();
        var idServico = execucao.Servicos[0].IdServico;

        var acao = () => execucao.FinalizarExecucaoServico(idServico);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void FinalizarExecucaoServico_UnicoServico_DeveFinalizarAgregadoERaiseDomainEvent()
    {
        var execucao = CriarComDiagnosticoFinalizado();
        var idServico = execucao.Servicos[0].IdServico;
        execucao.IniciarExecucaoServico(idServico);

        execucao.FinalizarExecucaoServico(idServico);

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.Finalizada);
        execucao.DataFinalizacao.Should().NotBeNull();
        execucao.DomainEvents.Should().ContainSingle(e => e is ExecucaoFinalizadaDomainEvent);
    }

    [Fact]
    public void FinalizarExecucaoServico_AindaComOutrosServicosPendentes_AgregadoContinuaEmExecucao()
    {
        var execucao = CriarComDiagnosticoFinalizado(quantidadeServicos: 2);
        var idServico1 = execucao.Servicos[0].IdServico;
        var idServico2 = execucao.Servicos[1].IdServico;

        execucao.IniciarExecucaoServico(idServico1);
        execucao.IniciarExecucaoServico(idServico2);
        execucao.FinalizarExecucaoServico(idServico1);

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.EmExecucao);
        execucao.Servicos.Should().Contain(s => s.Status == StatusItemServico.EmExecucao);
    }

    [Fact]
    public void Cancelar_QuandoEmDiagnostico_DeveTransicionarParaCanceladaEPublicarComDuranteDiagnosticoVerdadeiro()
    {
        var execucao = CriarEmDiagnostico();

        execucao.Cancelar("Veículo não atendível");

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.Cancelada);
        execucao.DataFinalizacao.Should().NotBeNull();
        execucao.MotivoCancelamento.Should().Be("Veículo não atendível");
        execucao.DomainEvents.Should().ContainSingle(e => e is ExecucaoCanceladaDomainEvent)
            .Which.Should().BeOfType<ExecucaoCanceladaDomainEvent>()
            .Which.DuranteDiagnostico.Should().BeTrue();
    }

    [Fact]
    public void Cancelar_QuandoEmExecucao_DevePublicarComDuranteDiagnosticoFalso()
    {
        var execucao = CriarComDiagnosticoFinalizado();
        var idServico = execucao.Servicos[0].IdServico;
        execucao.IniciarExecucaoServico(idServico);

        execucao.Cancelar("Peça indisponível");

        execucao.Status.Should().Be(StatusExecucaoOrdemServico.Cancelada);
        execucao.DomainEvents.Should().ContainSingle(e => e is ExecucaoCanceladaDomainEvent)
            .Which.Should().BeOfType<ExecucaoCanceladaDomainEvent>()
            .Which.DuranteDiagnostico.Should().BeFalse();
    }

    [Fact]
    public void Cancelar_QuandoJaFinalizada_DeveLancarDomainException()
    {
        var execucao = CriarComDiagnosticoFinalizado();
        var idServico = execucao.Servicos[0].IdServico;
        execucao.IniciarExecucaoServico(idServico);
        execucao.FinalizarExecucaoServico(idServico);

        var acao = () => execucao.Cancelar("Motivo qualquer");

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancelar_QuandoJaCancelada_DeveLancarDomainException()
    {
        var execucao = CriarEmDiagnostico();
        execucao.Cancelar("Primeiro cancelamento");

        var acao = () => execucao.Cancelar("Segundo cancelamento");

        acao.Should().Throw<DomainException>();
    }
}

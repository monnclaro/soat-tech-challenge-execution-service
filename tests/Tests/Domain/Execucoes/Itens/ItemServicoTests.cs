using Domain.Common.Exceptions;
using Domain.Execucoes.Itens;
using Domain.Execucoes.Itens.Enums;
using FluentAssertions;

namespace Tests.Domain.Execucoes.Itens;

// Cobre a máquina de estados portada de
// soat-tech-challenge/src/Domain/OrdensServico/Servicos/OrdemServicoServico.cs.
public class ItemServicoTests
{
    [Fact]
    public void Constructor_ComDadosValidos_DeveIniciarAguardandoExecucao()
    {
        var item = new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m);

        item.Status.Should().Be(StatusItemServico.AguardandoExecucao);
        item.DataInicioExecucao.Should().BeNull();
        item.DataFinalizacaoExecucao.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_ComNomeInvalido_DeveLancarDomainException(string? nome)
    {
        var acao = () => new ItemServico(Guid.NewGuid(), nome!, 100m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_ComValorNegativo_DeveLancarDomainException()
    {
        var acao = () => new ItemServico(Guid.NewGuid(), "Alinhamento", -10m);

        acao.Should().Throw<DomainException>()
            .WithMessage("*não pode ser negativo*");
    }

    [Fact]
    public void IniciarExecucao_DeveTransicionarParaEmExecucao_EDefinirDataInicio()
    {
        var item = new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m);

        item.IniciarExecucao();

        item.Status.Should().Be(StatusItemServico.EmExecucao);
        item.DataInicioExecucao.Should().NotBeNull();
    }

    [Fact]
    public void FinalizarExecucao_DeveTransicionarParaExecucaoFinalizada_EDefinirDataFinalizacao()
    {
        var item = new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m);
        item.IniciarExecucao();

        item.FinalizarExecucao();

        item.Status.Should().Be(StatusItemServico.ExecucaoFinalizada);
        item.DataFinalizacaoExecucao.Should().NotBeNull();
    }

    [Fact]
    public void IniciarExecucao_QuandoJaFinalizado_NaoDevePermitirIniciarNovamente()
    {
        var item = new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m);
        item.IniciarExecucao();
        item.FinalizarExecucao();

        var acao = () => item.IniciarExecucao();

        acao.Should().Throw<DomainException>()
            .WithMessage("O serviço já se encontra finalizado.");
    }

    [Fact]
    public void FinalizarExecucao_QuandoJaFinalizado_DeveLancarDomainException()
    {
        var item = new ItemServico(Guid.NewGuid(), "Troca de óleo", 150m);
        item.IniciarExecucao();
        item.FinalizarExecucao();

        var acao = () => item.FinalizarExecucao();

        acao.Should().Throw<DomainException>()
            .WithMessage("O serviço já se encontra finalizado.");
    }
}

using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Itens.Enums;
using FluentAssertions;
using Infrastructure.Database.Mappers;

namespace Tests.Infrastructure.Database.Mappers;

public class ExecucaoOrdemServicoMapperTests
{
    private static ExecucaoOrdemServico CriarExecucaoComItens()
    {
        var execucao = ExecucaoOrdemServico.Abrir(Guid.NewGuid());
        execucao.IniciarDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Troca de óleo", 100m);
        execucao.AdicionarProdutoDiagnosticado(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);
        execucao.FinalizarDiagnostico();
        return execucao;
    }

    [Fact]
    public void ToDocument_DeveMapearTodosOsCamposDoAgregadoEDosItens()
    {
        var execucao = CriarExecucaoComItens();

        var documento = ExecucaoOrdemServicoMapper.ToDocument(execucao);

        documento.IdOrdemServico.Should().Be(execucao.IdOrdemServico);
        documento.Status.Should().Be(StatusExecucaoOrdemServico.DiagnosticoFinalizado);
        documento.DataCriacao.Should().Be(execucao.DataCriacao);
        documento.DataFinalizacaoDiagnostico.Should().Be(execucao.DataFinalizacaoDiagnostico);

        documento.Servicos.Should().ContainSingle();
        var servicoOriginal = execucao.Servicos[0];
        var servicoDocumento = documento.Servicos[0];
        servicoDocumento.Id.Should().Be(servicoOriginal.Id);
        servicoDocumento.IdServico.Should().Be(servicoOriginal.IdServico);
        servicoDocumento.NomeServico.Should().Be("Troca de óleo");
        servicoDocumento.Valor.Should().Be(100m);
        servicoDocumento.Status.Should().Be(StatusItemServico.AguardandoExecucao);

        documento.Produtos.Should().ContainSingle();
        var produtoOriginal = execucao.Produtos[0];
        var produtoDocumento = documento.Produtos[0];
        produtoDocumento.Id.Should().Be(produtoOriginal.Id);
        produtoDocumento.IdProduto.Should().Be(produtoOriginal.IdProduto);
        produtoDocumento.NomeProduto.Should().Be("Filtro de óleo");
        produtoDocumento.ValorUnitario.Should().Be(50m);
        produtoDocumento.Quantidade.Should().Be(2m);
    }

    [Fact]
    public void ToDomain_DeveReidratarAgregadoEItensAPartirDoDocumento()
    {
        var execucaoOriginal = CriarExecucaoComItens();
        var documento = ExecucaoOrdemServicoMapper.ToDocument(execucaoOriginal);

        var execucaoReidratada = ExecucaoOrdemServicoMapper.ToDomain(documento);

        execucaoReidratada.IdOrdemServico.Should().Be(execucaoOriginal.IdOrdemServico);
        execucaoReidratada.Status.Should().Be(execucaoOriginal.Status);
        execucaoReidratada.DataCriacao.Should().Be(execucaoOriginal.DataCriacao);
        execucaoReidratada.DataFinalizacaoDiagnostico.Should().Be(execucaoOriginal.DataFinalizacaoDiagnostico);

        execucaoReidratada.Servicos.Should().ContainSingle();
        execucaoReidratada.Servicos[0].IdServico.Should().Be(execucaoOriginal.Servicos[0].IdServico);
        execucaoReidratada.Servicos[0].NomeServico.Should().Be(execucaoOriginal.Servicos[0].NomeServico);
        execucaoReidratada.Servicos[0].Status.Should().Be(execucaoOriginal.Servicos[0].Status);

        execucaoReidratada.Produtos.Should().ContainSingle();
        execucaoReidratada.Produtos[0].IdProduto.Should().Be(execucaoOriginal.Produtos[0].IdProduto);
        execucaoReidratada.Produtos[0].Subtotal.Should().Be(execucaoOriginal.Produtos[0].Subtotal);
    }

    [Fact]
    public void ToDocument_SemServicosOuProdutos_DeveMapearListasVazias()
    {
        var execucao = ExecucaoOrdemServico.Abrir(Guid.NewGuid());

        var documento = ExecucaoOrdemServicoMapper.ToDocument(execucao);

        documento.Servicos.Should().BeEmpty();
        documento.Produtos.Should().BeEmpty();
    }

    [Fact]
    public void ToDocumentEToDomain_ComExecucaoCancelada_DevePreservarOMotivoDeCancelamento()
    {
        var execucao = ExecucaoOrdemServico.Abrir(Guid.NewGuid());
        execucao.IniciarDiagnostico();
        execucao.Cancelar("Veículo não atendível");

        var documento = ExecucaoOrdemServicoMapper.ToDocument(execucao);
        documento.MotivoCancelamento.Should().Be("Veículo não atendível");

        var execucaoReidratada = ExecucaoOrdemServicoMapper.ToDomain(documento);
        execucaoReidratada.Status.Should().Be(StatusExecucaoOrdemServico.Cancelada);
        execucaoReidratada.MotivoCancelamento.Should().Be("Veículo não atendível");
    }
}

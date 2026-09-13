using Domain.Execucoes;
using Domain.Execucoes.Gateways;
using Domain.Execucoes.Itens.Enums;
using FluentAssertions;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging.Consumers;

// Cobre o comportamento não trivial do consumer: o comando IniciarExecucao só carrega o
// IdOrdemServico (sem detalhe por serviço), então o consumer busca seu próprio snapshot da
// ExecucaoOrdemServico, filtra os serviços ainda AguardandoExecucao e chama o
// IniciarExecucaoServicoUseCase uma vez por item pendente.
public class IniciarExecucaoConsumerTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly IniciarExecucaoConsumer _consumer;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();

    public IniciarExecucaoConsumerTests()
    {
        _consumer = new IniciarExecucaoConsumer(_gateway.Object);
    }

    private static Mock<ConsumeContext<IniciarExecucao>> CriarContexto(Guid idOrdemServico)
    {
        var context = new Mock<ConsumeContext<IniciarExecucao>>();
        context.Setup(c => c.Message).Returns(new IniciarExecucao(idOrdemServico));
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_QuandoExecucaoNaoExiste_DeveLancarInvalidOperationException()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        var contexto = CriarContexto(IdOrdemServico);

        var acao = async () => await _consumer.Consume(contexto.Object);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_ComMultiplosServicosPendentes_DeveIniciarExecucaoDeCadaUmENaoMexerNosDemais()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Serviço A", 100m);
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Serviço B", 200m);
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Serviço C (já finalizado)", 300m);
        execucao.FinalizarDiagnostico();

        var idServicoA = execucao.Servicos[0].IdServico;
        var idServicoB = execucao.Servicos[1].IdServico;
        var idServicoC = execucao.Servicos[2].IdServico;

        // Serviço C já percorreu (e concluiu) a execução antes deste comando chegar —
        // não deve ser tocado pelo consumer, que só deve iniciar A e B (AguardandoExecucao).
        execucao.IniciarExecucaoServico(idServicoC);
        execucao.FinalizarExecucaoServico(idServicoC);

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var contexto = CriarContexto(IdOrdemServico);

        await _consumer.Consume(contexto.Object);

        // Uma chamada a Atualizar por serviço pendente iniciado (A e B) — a asserção principal
        // do comportamento de "loop item a item" descrito no consumer.
        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        execucao.Servicos.Single(s => s.IdServico == idServicoA).Status.Should().Be(StatusItemServico.EmExecucao);
        execucao.Servicos.Single(s => s.IdServico == idServicoB).Status.Should().Be(StatusItemServico.EmExecucao);
        execucao.Servicos.Single(s => s.IdServico == idServicoC).Status.Should().Be(StatusItemServico.ExecucaoFinalizada);
    }

    [Fact]
    public async Task Consume_SemServicosPendentes_NaoDeveChamarAtualizar()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        execucao.IniciarDiagnostico();
        execucao.AdicionarServicoDiagnosticado(Guid.NewGuid(), "Serviço único", 100m);
        execucao.FinalizarDiagnostico();
        var idServico = execucao.Servicos[0].IdServico;
        execucao.IniciarExecucaoServico(idServico);
        execucao.FinalizarExecucaoServico(idServico);

        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        var contexto = CriarContexto(IdOrdemServico);

        await _consumer.Consume(contexto.Object);

        _gateway.Verify(g => g.Atualizar(It.IsAny<ExecucaoOrdemServico>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

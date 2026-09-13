using Domain.Execucoes;
using Domain.Execucoes.Enums;
using Domain.Execucoes.Gateways;
using FluentAssertions;
using Infrastructure.Messaging.Consumers;
using MassTransit;
using Moq;
using Soat.Contracts.Saga;

namespace Tests.Infrastructure.Messaging.Consumers;

// Consome o comando publicado pelo OS Service ao abrir uma OS, reaproveitando o
// IniciarDiagnosticoUseCase (mesma regra de negócio do endpoint REST equivalente).
public class IniciarDiagnosticoConsumerTests
{
    private readonly Mock<IExecucaoOrdemServicoGateway> _gateway = new();
    private readonly IniciarDiagnosticoConsumer _consumer;
    private static readonly Guid IdOrdemServico = Guid.NewGuid();
    private static readonly Guid IdCliente = Guid.NewGuid();
    private static readonly Guid IdVeiculo = Guid.NewGuid();

    public IniciarDiagnosticoConsumerTests()
    {
        _consumer = new IniciarDiagnosticoConsumer(_gateway.Object);
    }

    private static Mock<ConsumeContext<Soat.Contracts.Saga.IniciarDiagnostico>> CriarContexto()
    {
        var context = new Mock<ConsumeContext<Soat.Contracts.Saga.IniciarDiagnostico>>();
        context.Setup(c => c.Message).Returns(new Soat.Contracts.Saga.IniciarDiagnostico(IdOrdemServico, IdCliente, IdVeiculo));
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_QuandoExecucaoNaoExiste_DeveAbrirEIniciarDiagnosticoESalvar()
    {
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecucaoOrdemServico?)null);

        await _consumer.Consume(CriarContexto().Object);

        _gateway.Verify(g => g.Salvar(
            It.Is<ExecucaoOrdemServico>(e => e.IdOrdemServico == IdOrdemServico && e.Status == StatusExecucaoOrdemServico.EmDiagnostico),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_QuandoExecucaoJaExiste_DeveIniciarDiagnosticoEAtualizar()
    {
        var execucao = ExecucaoOrdemServico.Abrir(IdOrdemServico);
        _gateway.Setup(g => g.BuscarPorIdOrdemServico(IdOrdemServico, It.IsAny<CancellationToken>()))
            .ReturnsAsync(execucao);

        await _consumer.Consume(CriarContexto().Object);

        _gateway.Verify(g => g.Atualizar(
            It.Is<ExecucaoOrdemServico>(e => e.Status == StatusExecucaoOrdemServico.EmDiagnostico),
            It.IsAny<CancellationToken>()), Times.Once);
        execucao.Status.Should().Be(StatusExecucaoOrdemServico.EmDiagnostico);
    }
}

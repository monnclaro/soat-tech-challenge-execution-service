using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.IniciarDiagnostico;
using Domain.Execucoes.Gateways;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging.Consumers;

// Consome o comando publicado pelo OS Service ao abrir uma OS, reaproveitando o mesmo
// IniciarDiagnosticoUseCase usado pelo endpoint interno equivalente (única fonte de
// verdade da regra de negócio, independente do meio de entrada) — ver
// PLANO-FASE-4-MICROSSERVICOS.md. IdCliente/IdVeiculo do comando não são usados hoje
// (a fila de execução só precisa do IdOrdemServico), mas ficam disponíveis na mensagem
// caso a fila precise exibi-los no futuro.
public class IniciarDiagnosticoConsumer : IConsumer<IniciarDiagnostico>
{
    private readonly IExecucaoOrdemServicoGateway _gateway;

    public IniciarDiagnosticoConsumer(IExecucaoOrdemServicoGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task Consume(ConsumeContext<IniciarDiagnostico> context)
    {
        var outputPort = new NoopOutputPort();
        var useCase = new IniciarDiagnosticoUseCase(_gateway, outputPort);

        await useCase.Execute(new IniciarDiagnosticoInput(context.Message.IdOrdemServico), context.CancellationToken);
    }

    private sealed class NoopOutputPort : IIniciarDiagnosticoOutputPort
    {
        public void Ok(ExecucaoOutput output) { }
    }
}

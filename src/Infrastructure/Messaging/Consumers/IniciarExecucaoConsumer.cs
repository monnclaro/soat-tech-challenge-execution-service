using Application.Execucoes.UseCases;
using Application.Execucoes.UseCases.IniciarExecucaoServico;
using Domain.Execucoes.Gateways;
using Domain.Execucoes.Itens.Enums;
using MassTransit;
using Soat.Contracts.Saga;

namespace Infrastructure.Messaging.Consumers;

// Consome o comando publicado pelo OS Service ao aprovar o pagamento (evento
// PagamentoAprovado do Billing Service). O comando só carrega o IdOrdemServico (o OS Service não acompanha os serviços item a item); este
// serviço já tem seu próprio snapshot dos serviços diagnosticados, então inicia a
// execução de todos os que ainda estão AguardandoExecucao, reaproveitando o mesmo
// IniciarExecucaoServicoUseCase usado pelo endpoint interno equivalente (item a item, já
// que não existe um método de domínio para iniciar todos de uma vez).
public class IniciarExecucaoConsumer : IConsumer<IniciarExecucao>
{
    private readonly IExecucaoOrdemServicoGateway _gateway;

    public IniciarExecucaoConsumer(IExecucaoOrdemServicoGateway gateway)
    {
        _gateway = gateway;
    }

    public async Task Consume(ConsumeContext<IniciarExecucao> context)
    {
        var idOrdemServico = context.Message.IdOrdemServico;

        var execucao = await _gateway.BuscarPorIdOrdemServico(idOrdemServico, context.CancellationToken);
        if (execucao is null)
        {
            throw new InvalidOperationException($"Execução não encontrada para a ordem de serviço '{idOrdemServico}'.");
        }

        var idsServicosPendentes = execucao.Servicos
            .Where(s => s.Status == StatusItemServico.AguardandoExecucao)
            .Select(s => s.IdServico)
            .ToList();

        foreach (var idServico in idsServicosPendentes)
        {
            var outputPort = new NoopOutputPort();
            var useCase = new IniciarExecucaoServicoUseCase(_gateway, outputPort);

            await useCase.Execute(new IniciarExecucaoServicoInput(idOrdemServico, idServico), context.CancellationToken);
        }
    }

    private sealed class NoopOutputPort : IIniciarExecucaoServicoOutputPort
    {
        public void NaoEncontrado() { }
        public void Ok(ExecucaoOutput output) { }
    }
}

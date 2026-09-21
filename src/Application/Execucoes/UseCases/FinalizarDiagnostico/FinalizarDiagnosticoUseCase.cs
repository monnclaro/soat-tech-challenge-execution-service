using Application.Common.Interfaces;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.FinalizarDiagnostico;

// Recebe, num único payload, os serviços/produtos identificados durante o
// diagnóstico e fecha o diagnóstico — equivalente a InserirServicos/InserirProdutos
// + FinalizarDiagnostico do monolito de origem, num único caso de uso.
public class FinalizarDiagnosticoUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IFinalizarDiagnosticoOutputPort _outputPort;

    public FinalizarDiagnosticoUseCase(IExecucaoOrdemServicoGateway gateway, IFinalizarDiagnosticoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(FinalizarDiagnosticoInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            _outputPort.NaoEncontrado();
            return;
        }

        foreach (var servico in input.Servicos)
        {
            execucao.AdicionarServicoDiagnosticado(servico.IdServico, servico.NomeServico, servico.Valor);
        }

        foreach (var produto in input.Produtos)
        {
            execucao.AdicionarProdutoDiagnosticado(produto.IdProduto, produto.NomeProduto, produto.ValorUnitario, produto.Quantidade);
        }

        execucao.FinalizarDiagnostico();

        await _gateway.Atualizar(execucao, ct);

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

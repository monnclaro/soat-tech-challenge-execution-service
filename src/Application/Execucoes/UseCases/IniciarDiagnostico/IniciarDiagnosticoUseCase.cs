using Application.Common.Interfaces;
using Domain.Execucoes;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.IniciarDiagnostico;

// Em produção, este caso de uso seria disparado pelo comando IniciarDiagnostico
// publicado pelo OS Service ao criar uma OS (consumer de mensageria — não implementado
// ainda). Por ora é exposto via REST (PATCH /diagnostico/iniciar) com o mesmo efeito.
public class IniciarDiagnosticoUseCase : IUseCase
{
    private readonly IExecucaoOrdemServicoGateway _gateway;
    private readonly IIniciarDiagnosticoOutputPort _outputPort;

    public IniciarDiagnosticoUseCase(IExecucaoOrdemServicoGateway gateway, IIniciarDiagnosticoOutputPort outputPort)
    {
        _gateway = gateway;
        _outputPort = outputPort;
    }

    public async Task Execute(IniciarDiagnosticoInput input, CancellationToken ct = default)
    {
        var execucao = await _gateway.BuscarPorIdOrdemServico(input.IdOrdemServico, ct);

        if (execucao is null)
        {
            execucao = ExecucaoOrdemServico.Abrir(input.IdOrdemServico);
            execucao.IniciarDiagnostico();

            await _gateway.Salvar(execucao, ct);
        }
        else
        {
            execucao.IniciarDiagnostico();

            await _gateway.Atualizar(execucao, ct);
        }

        _outputPort.Ok(ExecucaoOutputMapper.Map(execucao));
    }
}

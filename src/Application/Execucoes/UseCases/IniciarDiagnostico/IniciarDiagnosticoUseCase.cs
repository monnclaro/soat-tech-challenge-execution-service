using Application.Common.Interfaces;
using Domain.Execucoes;
using Domain.Execucoes.Gateways;

namespace Application.Execucoes.UseCases.IniciarDiagnostico;

// Disparado pelo comando IniciarDiagnostico publicado pelo OS Service ao criar
// uma OS (IniciarDiagnosticoConsumer). Ao abrir a execução, o agregado levanta
// DiagnosticoIniciadoDomainEvent, que confirma de volta pro OS Service avançar
// a OS para EmDiagnostico — sem essa confirmação, a OS fica presa em Recebida.
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

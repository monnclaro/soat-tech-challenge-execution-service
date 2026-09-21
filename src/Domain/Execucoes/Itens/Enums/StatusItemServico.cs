namespace Domain.Execucoes.Itens.Enums;

// Máquina de estados por item de serviço, portada de
// soat-tech-challenge/src/Domain/OrdensServico/Servicos/Enums/StatusOrdemServicoServico.cs
// (a execução por item pertence a este serviço a partir da Fase 4).
public enum StatusItemServico
{
    AguardandoExecucao = 0,
    EmExecucao = 1,
    ExecucaoFinalizada = 2,
}

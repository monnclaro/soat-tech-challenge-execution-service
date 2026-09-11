namespace Application.Execucoes.UseCases.FinalizarExecucaoServico;

public interface IFinalizarExecucaoServicoOutputPort
{
    void NaoEncontrado();
    void Ok(ExecucaoOutput output);
}

namespace Application.Execucoes.UseCases.IniciarExecucaoServico;

public interface IIniciarExecucaoServicoOutputPort
{
    void NaoEncontrado();
    void Ok(ExecucaoOutput output);
}

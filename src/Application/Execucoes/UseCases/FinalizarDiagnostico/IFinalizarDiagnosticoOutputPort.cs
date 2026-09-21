namespace Application.Execucoes.UseCases.FinalizarDiagnostico;

public interface IFinalizarDiagnosticoOutputPort
{
    void NaoEncontrado();
    void Ok(ExecucaoOutput output);
}

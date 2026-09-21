namespace Application.Execucoes.UseCases.Cancelar;

public interface ICancelarOutputPort
{
    void NaoEncontrado();
    void Ok(ExecucaoOutput output);
}

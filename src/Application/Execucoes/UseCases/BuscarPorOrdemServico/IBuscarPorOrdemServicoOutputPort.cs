namespace Application.Execucoes.UseCases.BuscarPorOrdemServico;

public interface IBuscarPorOrdemServicoOutputPort
{
    void NaoEncontrado();
    void Ok(ExecucaoOutput output);
}

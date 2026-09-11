namespace Application.Execucoes.UseCases.BuscarFila;

public interface IBuscarFilaOutputPort
{
    void Ok(IReadOnlyList<ExecucaoOutput> fila);
}

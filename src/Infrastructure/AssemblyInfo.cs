using System.Runtime.CompilerServices;

// Permite que o projeto de testes acesse tipos internal ao assembly Infrastructure
// (ex.: ExecucaoOrdemServicoMapper), evitando torná-los public só por causa de testes.
[assembly: InternalsVisibleTo("Tests")]

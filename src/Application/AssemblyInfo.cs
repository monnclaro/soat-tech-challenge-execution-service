using System.Runtime.CompilerServices;

// Permite que o projeto de testes acesse os handlers de domain event e outros tipos
// internal ao assembly Application (ex.: PublicarDiagnosticoFinalizadoHandler,
// PublicarExecucaoFinalizadaHandler), evitando torná-los public só por causa de testes.
[assembly: InternalsVisibleTo("Tests")]

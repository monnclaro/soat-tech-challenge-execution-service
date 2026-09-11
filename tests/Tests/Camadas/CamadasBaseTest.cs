using System.Reflection;
using Application;
using Domain.Execucoes;
using Infrastructure.Database;

namespace Tests.Camadas;

public abstract class CamadasBaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(ExecucaoOrdemServico).Assembly;
    protected static readonly Assembly ApplicationAssembly = typeof(DependencyInjection).Assembly;
    protected static readonly Assembly InfrastructureAssembly = typeof(MongoContext).Assembly;
    protected static readonly Assembly PresentationAssembly = typeof(Program).Assembly;
}

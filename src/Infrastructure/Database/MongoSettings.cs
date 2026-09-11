using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Database;

// POCO de configuração (binding de appsettings), sem lógica — excluído da cobertura.
[ExcludeFromCodeCoverage]
public class MongoSettings
{
    public string ConnectionString { get; set; } = null!;
    public string Database { get; set; } = null!;
}

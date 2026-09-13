using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Messaging;

// POCO de configuração (binding de appsettings), sem lógica — excluído da cobertura.
[ExcludeFromCodeCoverage]
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
}

using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Security.Jwt;

// Este serviço nunca emite tokens (não há login/AuthenticationController aqui) — ele é
// um resource server puro que só valida o JWT emitido pelo OS Service / Lambda de auth,
// compartilhando o mesmo segredo simétrico (ver AuthExtensions.AddJwtAuthentication).
// POCO de configuração, sem lógica — excluído da cobertura.
[ExcludeFromCodeCoverage]
public class JwtSettings
{
    public string Secret { get; set; } = null!;
}

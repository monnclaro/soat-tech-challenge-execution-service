using System.Diagnostics.CodeAnalysis;
using System.Text;
using Infrastructure.Security.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Api.Extensions;

// Este serviço nunca emite JWT (não há AuthenticationController/login aqui) — é um
// resource server puro, validando o token emitido pelo OS Service / Lambda de auth com
// o mesmo segredo simétrico compartilhado (ADR 0005 do monolito de origem).
// Registro de infraestrutura, sem lógica de negócio/branching — excluído da cobertura.
[ExcludeFromCodeCoverage]
public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection("JwtSettings")
            .Get<JwtSettings>()!;

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services
            .AddAuthorization()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        return services;
    }
}

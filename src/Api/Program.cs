using System.Diagnostics.CodeAnalysis;
using Api.Extensions;
using Api.Middlewares;
using Application;
using Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Logs estruturados em JSON (CLEF) no stdout — o New Relic Kubernetes integration
// (infra-k8s) coleta via Fluent Bit e correlaciona pelo campo CorrelationId injetado
// por CorrelationIdMiddleware. Mesmo padrão já validado no monolito/OS Service.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddHealthChecks();

var app = builder.Build();

// Sem passo de inicialização de banco: MongoDB é schemaless (sem migrations/seed como
// no OS Service) — a coleção/índices são criados sob demanda pelo próprio driver.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapScalarApiReference();
app.MapHealthChecks("/health");

await app.RunAsync();

// Ponto de ancoragem público para WebApplicationFactory (testes de integração) e
// NetArchTest (testes de arquitetura) — top-level statements geram um Program
// internal por padrão. Composição do host, sem lógica de negócio própria — excluído
// da cobertura (não há branching que justifique um teste dedicado).
[ExcludeFromCodeCoverage]
public partial class Program;

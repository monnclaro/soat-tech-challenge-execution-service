using Api.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Tests.Api.Middlewares;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ComHeaderPresente_DeveReutilizarOMesmoCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlation-existente";

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().Should().Be("correlation-existente");
    }

    [Fact]
    public async Task InvokeAsync_SemHeader_DeveGerarNovoCorrelationId()
    {
        var context = new DefaultHttpContext();

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_DeveChamarOProximoMiddleware()
    {
        var context = new DefaultHttpContext();
        var chamouProximo = false;

        var middleware = new CorrelationIdMiddleware(_ =>
        {
            chamouProximo = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        chamouProximo.Should().BeTrue();
    }
}

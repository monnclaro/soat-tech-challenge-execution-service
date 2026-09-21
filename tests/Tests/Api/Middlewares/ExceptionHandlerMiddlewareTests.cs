using System.Text;
using System.Text.Json;
using Api.Common.Exceptions;
using Api.Middlewares;
using Domain.Common.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Api.Middlewares;

public class ExceptionHandlerMiddlewareTests
{
    private static async Task<(int StatusCode, string? Erro)> Executar(RequestDelegate next)
    {
        var middleware = new ExceptionHandlerMiddleware(next, NullLogger<ExceptionHandlerMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        string? erro = null;
        if (!string.IsNullOrEmpty(body))
        {
            using var document = JsonDocument.Parse(body);
            erro = document.RootElement.GetProperty("erro").GetString();
        }

        return (context.Response.StatusCode, erro);
    }

    [Fact]
    public async Task InvokeAsync_SemExcecao_DeveApenasChamarProximoMiddleware()
    {
        var chamouProximo = false;
        var (statusCode, _) = await Executar(_ =>
        {
            chamouProximo = true;
            return Task.CompletedTask;
        });

        chamouProximo.Should().BeTrue();
        statusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_ComDomainException_DeveRetornar400ComMensagem()
    {
        var (statusCode, erro) = await Executar(_ => throw new DomainException("regra de negócio violada"));

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        erro.Should().Be("regra de negócio violada");
    }

    [Fact]
    public async Task InvokeAsync_ComNotFoundException_DeveRetornar404ComMensagem()
    {
        var (statusCode, erro) = await Executar(_ => throw new NotFoundException("não encontrado"));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        erro.Should().Be("não encontrado");
    }

    [Fact]
    public async Task InvokeAsync_ComConflictException_DeveRetornar409ComMensagem()
    {
        var (statusCode, erro) = await Executar(_ => throw new ConflictException("conflito"));

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        erro.Should().Be("conflito");
    }

    [Fact]
    public async Task InvokeAsync_ComExcecaoInesperada_DeveRetornar500ComMensagemGenerica()
    {
        var (statusCode, erro) = await Executar(_ => throw new InvalidOperationException("boom"));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        erro.Should().Be("Ocorreu um erro interno. Tente novamente.");
    }
}

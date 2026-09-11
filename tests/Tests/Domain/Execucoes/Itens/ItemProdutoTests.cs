using Domain.Common.Exceptions;
using Domain.Execucoes.Itens;
using FluentAssertions;

namespace Tests.Domain.Execucoes.Itens;

public class ItemProdutoTests
{
    [Fact]
    public void Constructor_ComDadosValidos_DeveCalcularSubtotal()
    {
        var item = new ItemProduto(Guid.NewGuid(), "Filtro de óleo", 50m, 2m);

        item.Subtotal.Should().Be(100m);
    }

    [Fact]
    public void Constructor_ComQuantidadeZero_DeveLancarDomainException()
    {
        var acao = () => new ItemProduto(Guid.NewGuid(), "Filtro de óleo", 50m, 0m);

        acao.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_ComValorUnitarioNegativo_DeveLancarDomainException()
    {
        var acao = () => new ItemProduto(Guid.NewGuid(), "Filtro de óleo", -1m, 2m);

        acao.Should().Throw<DomainException>();
    }
}

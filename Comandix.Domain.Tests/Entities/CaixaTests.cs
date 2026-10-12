using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Tests.Entities;

public class CaixaTests
{
    private static readonly Guid OperadorId = Guid.NewGuid();

    [Fact]
    public void Abrir_ComFundoNegativo_DeveLancarExcecao()
    {
        Assert.Throws<DomainException>(() => new Caixa(OperadorId, -1m));
    }

    [Fact]
    public void RegistrarMovimentacao_ComValorZero_DeveLancarExcecao()
    {
        var caixa = new Caixa(OperadorId, 100m);

        Assert.Throws<DomainException>(() => caixa.RegistrarSangria(0m, "Cofre", OperadorId));
    }

    [Fact]
    public void RegistrarSangria_SemMotivo_DeveLancarExcecao()
    {
        var caixa = new Caixa(OperadorId, 100m);

        Assert.Throws<DomainException>(() => caixa.RegistrarSangria(50m, "  ", OperadorId));
    }

    [Fact]
    public void Fechar_DeveCalcularDinheiroEsperadoEDiferenca()
    {
        var caixa = new Caixa(OperadorId, 100m);
        caixa.RegistrarSuprimento(50m, "Reforço de troco", OperadorId);
        caixa.RegistrarSangria(30m, "Cofre", OperadorId);

        caixa.Fechar(totalVendasDinheiro: 200m, valorContado: 310m, OperadorId);

        Assert.Equal(StatusCaixa.Fechado, caixa.Status);
        Assert.Equal(320m, caixa.ValorEsperadoDinheiro);
        Assert.Equal(-10m, caixa.Diferenca);
    }

    [Fact]
    public void CaixaFechado_NaoAceitaMovimentacao()
    {
        var caixa = new Caixa(OperadorId, 100m);
        caixa.Fechar(0m, 100m, OperadorId);

        Assert.Throws<DomainException>(() => caixa.RegistrarSuprimento(10m, "Troco", OperadorId));
    }
}

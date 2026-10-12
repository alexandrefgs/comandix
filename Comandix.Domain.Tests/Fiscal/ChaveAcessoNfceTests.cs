using Comandix.Domain.Fiscal;

namespace Comandix.Domain.Tests.Fiscal;

public class ChaveAcessoNfceTests
{
    [Theory]
    [InlineData("0000000000000000000000000000000000000000000", 0)]
    [InlineData("0000000000000000000000000000000000000000001", 9)]
    [InlineData("0000000000000000000000000000000000000000010", 8)]
    public void CalcularDigitoVerificador_DeveSeguirModulo11(string chave, int esperado)
    {
        Assert.Equal(esperado, ChaveAcessoNfce.CalcularDigitoVerificador(chave));
    }

    [Fact]
    public void Gerar_DeveMontarChaveDe44DigitosComDigitoValido()
    {
        var chave = ChaveAcessoNfce.Gerar(
            42,
            new DateTime(2026, 10, 12),
            "00000000000000",
            1,
            123,
            48213907
        );

        Assert.Equal(44, chave.Length);
        Assert.True(chave.All(char.IsDigit));
        Assert.Equal("42", chave[..2]);
        Assert.Equal("2610", chave.Substring(2, 4));
        Assert.Equal("65", chave.Substring(20, 2));
        Assert.Equal("000000123", chave.Substring(25, 9));
        Assert.Equal(ChaveAcessoNfce.CalcularDigitoVerificador(chave[..43]), chave[43] - '0');
    }

    [Fact]
    public void Gerar_ComCnpjInvalido_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() =>
            ChaveAcessoNfce.Gerar(42, DateTime.Today, "123", 1, 1, 12345678)
        );
    }
}

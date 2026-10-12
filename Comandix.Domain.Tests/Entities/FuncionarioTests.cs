using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Tests.Entities;

public class FuncionarioTests
{
    [Theory]
    [InlineData(Cargo.Garcom)]
    [InlineData(Cargo.Cozinha)]
    public void DefinirCredenciais_ParaCargoSemSenha_DeveLancarExcecao(Cargo cargo)
    {
        var funcionario = new Funcionario("Carlos", cargo);

        Assert.Throws<DomainException>(() => funcionario.DefinirCredenciais("carlos", "hash"));
    }

    [Fact]
    public void DefinirCredenciais_DeveNormalizarUsuario()
    {
        var funcionario = new Funcionario("Maria", Cargo.Caixa);

        funcionario.DefinirCredenciais("  Maria.Caixa ", "hash");

        Assert.Equal("maria.caixa", funcionario.Usuario);
    }

    [Fact]
    public void Atualizar_ParaCargoSemSenha_DeveRemoverCredenciais()
    {
        var funcionario = new Funcionario("Maria", Cargo.Caixa);
        funcionario.DefinirCredenciais("maria", "hash");

        funcionario.Atualizar("Maria", Cargo.Garcom);

        Assert.Null(funcionario.Usuario);
        Assert.Null(funcionario.SenhaHash);
    }

    [Theory]
    [InlineData(Cargo.Gerente)]
    [InlineData(Cargo.Caixa)]
    public void DefinirCodigoAcesso_ParaCargoComSenha_DeveLancarExcecao(Cargo cargo)
    {
        var funcionario = new Funcionario("Ana", cargo);

        Assert.Throws<DomainException>(() => funcionario.DefinirCodigoAcesso("hash"));
    }

    [Fact]
    public void DefinirPin_SemCodigoAcesso_DeveLancarExcecao()
    {
        var garcom = new Funcionario("Carlos", Cargo.Garcom);

        Assert.Throws<DomainException>(() => garcom.DefinirPin("pin"));
    }

    [Fact]
    public void DefinirPin_QuandoJaDefinido_DeveLancarExcecao()
    {
        var garcom = GarcomComPin();

        Assert.Throws<DomainException>(() => garcom.DefinirPin("outro-pin"));
    }

    [Fact]
    public void RegistrarFalhaPin_DeveBloquearSomenteNaQuintaTentativa()
    {
        var garcom = GarcomComPin();

        for (var i = 0; i < Funcionario.MaximoTentativasPin - 1; i++)
            garcom.RegistrarFalhaPin();

        Assert.False(garcom.AcessoBloqueado);

        garcom.RegistrarFalhaPin();

        Assert.True(garcom.AcessoBloqueado);
    }

    [Fact]
    public void RegistrarSucessoPin_DeveZerarTentativas()
    {
        var garcom = GarcomComPin();
        garcom.RegistrarFalhaPin();
        garcom.RegistrarFalhaPin();

        garcom.RegistrarSucessoPin();

        Assert.Equal(0, garcom.TentativasPinFalhas);
    }

    [Fact]
    public void ResetarPin_DeveLimparPinEDesbloquear()
    {
        var garcom = GarcomComPin();
        for (var i = 0; i < Funcionario.MaximoTentativasPin; i++)
            garcom.RegistrarFalhaPin();

        garcom.ResetarPin();

        Assert.False(garcom.PossuiPin);
        Assert.False(garcom.AcessoBloqueado);
        Assert.Equal(0, garcom.TentativasPinFalhas);
    }

    [Fact]
    public void DefinirCodigoAcesso_AoRegenerar_DeveManterPin()
    {
        var garcom = GarcomComPin();

        garcom.DefinirCodigoAcesso("novo-codigo");

        Assert.Equal("novo-codigo", garcom.CodigoAcessoHash);
        Assert.True(garcom.PossuiPin);
    }

    [Fact]
    public void Atualizar_ParaCargoComSenha_DeveRemoverAcessoPorQrCode()
    {
        var garcom = GarcomComPin();

        garcom.Atualizar("Carlos", Cargo.Caixa);

        Assert.Null(garcom.CodigoAcessoHash);
        Assert.False(garcom.PossuiPin);
    }

    private static Funcionario GarcomComPin()
    {
        var garcom = new Funcionario("Carlos", Cargo.Garcom);
        garcom.DefinirCodigoAcesso("codigo");
        garcom.DefinirPin("pin");
        return garcom;
    }
}

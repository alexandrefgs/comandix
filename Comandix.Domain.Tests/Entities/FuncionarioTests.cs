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
}
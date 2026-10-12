using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Tests.Entities;

public class MesaTests
{
    [Fact]
    public void Atualizar_MesaLivre_DeveAlterarNumeroECapacidade()
    {
        var mesa = new Mesa(1, 4);

        mesa.Atualizar(10, 6);

        Assert.Equal(10, mesa.Numero);
        Assert.Equal(6, mesa.Capacidade);
    }

    [Fact]
    public void Atualizar_MesaOcupada_DeveLancarExcecao()
    {
        var mesa = new Mesa(1, 4);
        _ = new Comanda(mesa, new Funcionario("Carlos", Cargo.Garcom));

        var excecao = Assert.Throws<DomainException>(() => mesa.Atualizar(2, 6));

        Assert.Equal("Não é possível editar uma mesa ocupada.", excecao.Message);
    }
}
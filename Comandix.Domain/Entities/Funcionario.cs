using Comandix.Domain.Enums;

namespace Comandix.Domain.Entities;

public class Funcionario : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public Cargo Cargo { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected Funcionario() { }

    public Funcionario(string nome, Cargo cargo)
    {
        Nome = nome;
        Cargo = cargo;
    }

    public void Atualizar(string nome, Cargo cargo)
    {
        Nome = nome;
        Cargo = cargo;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}

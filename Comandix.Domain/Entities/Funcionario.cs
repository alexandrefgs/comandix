using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class Funcionario : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public Cargo Cargo { get; private set; }
    public bool Ativo { get; private set; } = true;
    public string? Usuario { get; private set; }
    public string? SenhaHash { get; private set; }

    public bool AcessaPorSenha => Cargo is Cargo.Gerente or Cargo.Caixa;

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

        if (!AcessaPorSenha)
        {
            Usuario = null;
            SenhaHash = null;
        }
    }

    public void DefinirCredenciais(string usuario, string senhaHash)
    {
        if (!AcessaPorSenha)
            throw new DomainException("Apenas gerente e caixa acessam com usuário e senha.");

        Usuario = usuario.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
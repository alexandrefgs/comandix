using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class Funcionario : EntityBase
{
    public const int MaximoTentativasPin = 5;

    public string Nome { get; private set; } = string.Empty;
    public Cargo Cargo { get; private set; }
    public bool Ativo { get; private set; } = true;

    public string? Usuario { get; private set; }
    public string? SenhaHash { get; private set; }

    public string? CodigoAcessoHash { get; private set; }
    public string? PinHash { get; private set; }
    public int TentativasPinFalhas { get; private set; }
    public bool AcessoBloqueado { get; private set; }

    public bool AcessaPorSenha => Cargo is Cargo.Gerente or Cargo.Caixa;
    public bool AcessaPorQrCode => Cargo is Cargo.Garcom or Cargo.Cozinha;
    public bool PossuiPin => PinHash is not null;

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

        if (!AcessaPorQrCode)
        {
            CodigoAcessoHash = null;
            LimparPin();
        }
    }

    public void DefinirCredenciais(string usuario, string senhaHash)
    {
        if (!AcessaPorSenha)
            throw new DomainException("Apenas gerente e caixa acessam com usuário e senha.");

        Usuario = usuario.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
    }

    public void DefinirCodigoAcesso(string codigoAcessoHash)
    {
        if (!AcessaPorQrCode)
            throw new DomainException("Apenas garçom e cozinha acessam por QR Code.");

        CodigoAcessoHash = codigoAcessoHash;
    }

    public void DefinirPin(string pinHash)
    {
        if (CodigoAcessoHash is null)
            throw new DomainException("O funcionário não possui código de acesso.");

        if (PossuiPin)
            throw new DomainException("O PIN já foi definido. Peça ao gerente para resetá-lo.");

        PinHash = pinHash;
        TentativasPinFalhas = 0;
    }

    public void RegistrarFalhaPin()
    {
        TentativasPinFalhas++;

        if (TentativasPinFalhas >= MaximoTentativasPin)
            AcessoBloqueado = true;
    }

    public void RegistrarSucessoPin() => TentativasPinFalhas = 0;

    public void Desbloquear()
    {
        AcessoBloqueado = false;
        TentativasPinFalhas = 0;
    }

    public void ResetarPin() => LimparPin();

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    private void LimparPin()
    {
        PinHash = null;
        TentativasPinFalhas = 0;
        AcessoBloqueado = false;
    }
}
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class Mesa : EntityBase
{
    public int Numero { get; private set; }
    public int Capacidade { get; private set; }
    public bool Ativa { get; private set; } = true;
    public StatusMesa Status { get; private set; } = StatusMesa.Livre;

    protected Mesa() { }

    public Mesa(int numero, int capacidade)
    {
        Numero = numero;
        Capacidade = capacidade;
    }

    public void Atualizar(int numero, int capacidade)
    {
        Numero = numero;
        Capacidade = capacidade;
    }

    public void Ativar() => Ativa = true;

    public void Desativar()
    {
        if (Status == StatusMesa.Ocupada)
            throw new DomainException("Não é possível desativar uma mesa ocupada.");

        Ativa = false;
    }

    internal void Ocupar()
    {
        if (!Ativa)
            throw new DomainException("A mesa está desativada.");

        if (Status == StatusMesa.Ocupada)
            throw new DomainException("A mesa já está ocupada.");

        Status = StatusMesa.Ocupada;
    }

    internal void Liberar() => Status = StatusMesa.Livre;
}
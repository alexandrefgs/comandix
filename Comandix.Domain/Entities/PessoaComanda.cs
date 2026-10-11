namespace Comandix.Domain.Entities;

public class PessoaComanda : EntityBase
{
    public Guid ComandaId { get; private set; }
    public int Ordem { get; private set; }
    public string Nome { get; private set; } = string.Empty;

    protected PessoaComanda() { }

    internal PessoaComanda(int ordem, string nome)
    {
        Ordem = ordem;
        Nome = nome;
    }

    internal void Renomear(string nome) => Nome = nome;
}

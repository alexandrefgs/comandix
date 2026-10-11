namespace Comandix.Domain.Entities;

public class Categoria : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; } = true;
    public ICollection<Produto> Produtos { get; private set; } = new List<Produto>();

    protected Categoria() { }

    public Categoria(string nome)
    {
        Nome = nome;
    }

    public void Atualizar(string nome) => Nome = nome;
    public void Ativar() => Ativa = true;
    public void Desativar() => Ativa = false;
}
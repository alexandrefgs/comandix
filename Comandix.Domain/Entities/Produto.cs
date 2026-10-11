namespace Comandix.Domain.Entities;

public class Produto : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal Preco { get; private set; }
    public bool Disponivel { get; private set; } = true;
    public Guid CategoriaId { get; private set; }
    public Categoria Categoria { get; private set; } = null!;

    protected Produto() { }

    public Produto(string nome, string? descricao, decimal preco, Guid categoriaId)
    {
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        CategoriaId = categoriaId;
    }

    public void Atualizar(string nome, string? descricao, decimal preco, Guid categoriaId)
    {
        Nome = nome;
        Descricao = descricao;
        Preco = preco;
        CategoriaId = categoriaId;
    }

    public void MarcarDisponivel() => Disponivel = true;
    public void MarcarIndisponivel() => Disponivel = false;
}
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class ItemComanda : EntityBase
{
    private readonly List<PessoaComanda> _pagantes = new();

    public Guid ComandaId { get; private set; }
    public Guid ProdutoId { get; private set; }
    public Produto Produto { get; private set; } = null!;
    public string NomeProduto { get; private set; } = string.Empty;
    public decimal PrecoUnitario { get; private set; }
    public int Quantidade { get; private set; }
    public string? Observacao { get; private set; }
    public StatusItem Status { get; private set; } = StatusItem.Pendente;
    public IReadOnlyCollection<PessoaComanda> Pagantes => _pagantes;

    public decimal Subtotal => PrecoUnitario * Quantidade;

    protected ItemComanda() { }

    internal ItemComanda(Produto produto, int quantidade, string? observacao)
    {
        ProdutoId = produto.Id;
        NomeProduto = produto.Nome;
        PrecoUnitario = produto.Preco;
        Quantidade = quantidade;
        Observacao = observacao;
    }

    public void AvancarStatus()
    {
        Status = Status switch
        {
            StatusItem.Pendente => StatusItem.EmPreparo,
            StatusItem.EmPreparo => StatusItem.Pronto,
            StatusItem.Pronto => StatusItem.Entregue,
            _ => throw new DomainException("Este item não pode avançar de status.")
        };
    }

    internal void Cancelar()
    {
        if (Status is StatusItem.Entregue or StatusItem.Cancelado)
            throw new DomainException("Não é possível cancelar um item entregue ou já cancelado.");

        Status = StatusItem.Cancelado;
        _pagantes.Clear();
    }

    internal void DefinirPagantes(IEnumerable<PessoaComanda> pessoas)
    {
        if (Status == StatusItem.Cancelado)
            throw new DomainException("Não é possível atribuir um item cancelado.");

        _pagantes.Clear();
        _pagantes.AddRange(pessoas);
    }

    internal void RemoverPagante(PessoaComanda pessoa) => _pagantes.Remove(pessoa);
}
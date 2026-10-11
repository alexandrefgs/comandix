using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;
using Comandix.Domain.ValueObjects;

namespace Comandix.Domain.Entities;

public class Comanda : EntityBase
{
    public const decimal PercentualTaxaServicoPadrao = 10m;

    private readonly List<ItemComanda> _itens = new();
    private readonly List<PessoaComanda> _pessoas = new();

    public Guid MesaId { get; private set; }
    public Mesa Mesa { get; private set; } = null!;
    public Guid GarcomId { get; private set; }
    public Funcionario Garcom { get; private set; } = null!;
    public StatusComanda Status { get; private set; } = StatusComanda.Aberta;
    public DateTime? FechadaEm { get; private set; }
    public bool CobrarTaxaServico { get; private set; } = true;
    public decimal PercentualTaxaServico { get; private set; }
    public decimal ValorTaxaServico { get; private set; }
    public IReadOnlyCollection<ItemComanda> Itens => _itens;
    public IReadOnlyCollection<PessoaComanda> Pessoas => _pessoas;

    public decimal Consumo =>
        _itens.Where(i => i.Status != StatusItem.Cancelado).Sum(i => i.Subtotal);

    public decimal TotalComTaxa => Consumo + CalcularTaxaServico();

    protected Comanda() { }

    public Comanda(
        Mesa mesa,
        Funcionario garcom,
        decimal percentualTaxaServico = PercentualTaxaServicoPadrao
    )
    {
        if (!garcom.Ativo || garcom.Cargo != Cargo.Garcom)
            throw new DomainException("A comanda deve ser aberta por um garçom ativo.");

        if (percentualTaxaServico < 0 || percentualTaxaServico > 100)
            throw new DomainException("O percentual da taxa de serviço é inválido.");

        mesa.Ocupar();
        MesaId = mesa.Id;
        Mesa = mesa;
        GarcomId = garcom.Id;
        Garcom = garcom;
        PercentualTaxaServico = percentualTaxaServico;
        _pessoas.Add(new PessoaComanda(1, "Pessoa 1"));
    }

    public ItemComanda AdicionarItem(Produto produto, int quantidade, string? observacao)
    {
        GarantirAberta();

        if (!produto.Disponivel)
            throw new DomainException($"O produto {produto.Nome} está indisponível.");

        if (quantidade <= 0)
            throw new DomainException("A quantidade deve ser maior que zero.");

        var item = new ItemComanda(produto, quantidade, observacao);
        _itens.Add(item);
        return item;
    }

    public void CancelarItem(Guid itemId)
    {
        GarantirAberta();
        ObterItem(itemId).Cancelar();
    }

    public void AvancarStatusItem(Guid itemId)
    {
        GarantirAberta();
        ObterItem(itemId).AvancarStatus();
    }

    public void DefinirCobrancaTaxaServico(bool cobrar)
    {
        GarantirAberta();
        CobrarTaxaServico = cobrar;
    }

    public void DefinirQuantidadePessoas(int quantidade)
    {
        GarantirAberta();

        if (quantidade < 1)
            throw new DomainException("A comanda precisa ter pelo menos 1 pessoa.");

        while (_pessoas.Count < quantidade)
        {
            var ordem = _pessoas.Max(p => p.Ordem) + 1;
            _pessoas.Add(new PessoaComanda(ordem, $"Pessoa {ordem}"));
        }

        while (_pessoas.Count > quantidade)
        {
            var ultima = _pessoas.MaxBy(p => p.Ordem)!;

            foreach (var item in _itens)
                item.RemoverPagante(ultima);

            _pessoas.Remove(ultima);
        }
    }

    public void RenomearPessoa(Guid pessoaId, string nome)
    {
        GarantirAberta();
        ObterPessoa(pessoaId).Renomear(nome);
    }

    public void AtribuirItem(Guid itemId, IEnumerable<Guid> pessoaIds)
    {
        GarantirAberta();
        var pessoas = pessoaIds.Distinct().Select(ObterPessoa).ToList();
        ObterItem(itemId).DefinirPagantes(pessoas);
    }

    public IReadOnlyList<DivisaoPessoa> CalcularDivisao()
    {
        var pessoas = _pessoas.OrderBy(p => p.Ordem).ToList();
        var consumos = pessoas.ToDictionary(p => p.Id, _ => 0m);

        foreach (var item in _itens.Where(i => i.Status != StatusItem.Cancelado))
        {
            var pagantes =
                item.Pagantes.Count > 0 ? item.Pagantes.OrderBy(p => p.Ordem).ToList() : pessoas;

            var partes = Ratear(item.Subtotal, pagantes.Count);

            for (var i = 0; i < pagantes.Count; i++)
                consumos[pagantes[i].Id] += partes[i];
        }

        return pessoas
            .Select(p =>
            {
                var consumo = consumos[p.Id];
                var taxa = CobrarTaxaServico
                    ? Math.Round(
                        consumo * PercentualTaxaServico / 100,
                        2,
                        MidpointRounding.AwayFromZero
                    )
                    : 0m;

                return new DivisaoPessoa(p.Id, p.Nome, consumo, taxa, consumo + taxa);
            })
            .ToList();
    }

    public decimal CalcularTaxaServico() => CalcularDivisao().Sum(d => d.TaxaServico);

    public void Fechar()
    {
        GarantirAberta();

        if (!_itens.Any(i => i.Status != StatusItem.Cancelado))
            throw new DomainException("A comanda não tem itens. Cancele-a em vez de fechar.");

        if (_itens.Any(i => i.Status is not (StatusItem.Entregue or StatusItem.Cancelado)))
            throw new DomainException("Ainda há itens que não foram entregues.");

        ValorTaxaServico = CalcularTaxaServico();
        Status = StatusComanda.Fechada;
        FechadaEm = DateTime.UtcNow;
        Mesa.Liberar();
    }

    public void Cancelar()
    {
        GarantirAberta();

        if (_itens.Any(i => i.Status != StatusItem.Cancelado))
            throw new DomainException("Cancele todos os itens antes de cancelar a comanda.");

        ValorTaxaServico = 0;
        Status = StatusComanda.Cancelada;
        FechadaEm = DateTime.UtcNow;
        Mesa.Liberar();
    }

    private void GarantirAberta()
    {
        if (Status != StatusComanda.Aberta)
            throw new DomainException("A comanda não está aberta.");
    }

    private ItemComanda ObterItem(Guid itemId) =>
        _itens.FirstOrDefault(i => i.Id == itemId)
        ?? throw new DomainException("Item não encontrado na comanda.");

    private PessoaComanda ObterPessoa(Guid pessoaId) =>
        _pessoas.FirstOrDefault(p => p.Id == pessoaId)
        ?? throw new DomainException("Pessoa não encontrada na comanda.");

    private static decimal[] Ratear(decimal valor, int partes)
    {
        var parteBase = Math.Floor(valor / partes * 100) / 100;
        var resultado = Enumerable.Repeat(parteBase, partes).ToArray();
        var sobra = valor - parteBase * partes;

        for (var i = 0; sobra > 0; i++, sobra -= 0.01m)
            resultado[i] += 0.01m;

        return resultado;
    }
}
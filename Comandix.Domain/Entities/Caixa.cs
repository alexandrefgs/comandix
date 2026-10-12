using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class Caixa : EntityBase
{
    private readonly List<MovimentacaoCaixa> _movimentacoes = new();

    public Guid AbertoPorId { get; private set; }
    public decimal ValorAbertura { get; private set; }
    public StatusCaixa Status { get; private set; } = StatusCaixa.Aberto;
    public DateTime? FechadoEm { get; private set; }
    public Guid? FechadoPorId { get; private set; }
    public decimal? ValorEsperadoDinheiro { get; private set; }
    public decimal? ValorContadoDinheiro { get; private set; }
    public IReadOnlyCollection<MovimentacaoCaixa> Movimentacoes => _movimentacoes;

    public decimal TotalSuprimentos =>
        _movimentacoes.Where(m => m.Tipo == TipoMovimentacaoCaixa.Suprimento).Sum(m => m.Valor);

    public decimal TotalSangrias =>
        _movimentacoes.Where(m => m.Tipo == TipoMovimentacaoCaixa.Sangria).Sum(m => m.Valor);

    public decimal? Diferenca => ValorContadoDinheiro - ValorEsperadoDinheiro;

    protected Caixa() { }

    public Caixa(Guid abertoPorId, decimal valorAbertura)
    {
        if (valorAbertura < 0)
            throw new DomainException("O fundo de troco não pode ser negativo.");

        AbertoPorId = abertoPorId;
        ValorAbertura = valorAbertura;
    }

    public decimal CalcularDinheiroEsperado(decimal totalVendasDinheiro) =>
        ValorAbertura + TotalSuprimentos - TotalSangrias + totalVendasDinheiro;

    public MovimentacaoCaixa RegistrarSuprimento(
        decimal valor,
        string motivo,
        Guid funcionarioId
    ) => RegistrarMovimentacao(TipoMovimentacaoCaixa.Suprimento, valor, motivo, funcionarioId);

    public MovimentacaoCaixa RegistrarSangria(decimal valor, string motivo, Guid funcionarioId) =>
        RegistrarMovimentacao(TipoMovimentacaoCaixa.Sangria, valor, motivo, funcionarioId);

    public void Fechar(decimal totalVendasDinheiro, decimal valorContado, Guid funcionarioId)
    {
        GarantirAberto();

        if (valorContado < 0)
            throw new DomainException("O valor contado não pode ser negativo.");

        ValorEsperadoDinheiro = CalcularDinheiroEsperado(totalVendasDinheiro);
        ValorContadoDinheiro = valorContado;
        FechadoPorId = funcionarioId;
        FechadoEm = DateTime.UtcNow;
        Status = StatusCaixa.Fechado;
    }

    private MovimentacaoCaixa RegistrarMovimentacao(
        TipoMovimentacaoCaixa tipo,
        decimal valor,
        string motivo,
        Guid funcionarioId
    )
    {
        GarantirAberto();

        if (valor <= 0)
            throw new DomainException("O valor deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Informe o motivo da movimentação.");

        var movimentacao = new MovimentacaoCaixa(tipo, valor, motivo.Trim(), funcionarioId);
        _movimentacoes.Add(movimentacao);
        return movimentacao;
    }

    private void GarantirAberto()
    {
        if (Status != StatusCaixa.Aberto)
            throw new DomainException("O caixa está fechado.");
    }
}

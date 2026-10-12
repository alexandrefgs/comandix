using Comandix.Domain.Enums;

namespace Comandix.Domain.Entities;

public class MovimentacaoCaixa : EntityBase
{
    public Guid CaixaId { get; private set; }
    public TipoMovimentacaoCaixa Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid FuncionarioId { get; private set; }

    protected MovimentacaoCaixa() { }

    internal MovimentacaoCaixa(
        TipoMovimentacaoCaixa tipo,
        decimal valor,
        string motivo,
        Guid funcionarioId
    )
    {
        Tipo = tipo;
        Valor = valor;
        Motivo = motivo;
        FuncionarioId = funcionarioId;
    }
}

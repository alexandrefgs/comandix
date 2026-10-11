using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Entities;

public class Pagamento : EntityBase
{
    public Guid ComandaId { get; private set; }
    public Guid CaixaId { get; private set; }
    public Guid? PessoaId { get; private set; }
    public FormaPagamento Forma { get; private set; }
    public decimal Valor { get; private set; }
    public decimal ValorRecebido { get; private set; }
    public Guid RegistradoPorId { get; private set; }
    public bool Estornado { get; private set; }
    public DateTime? EstornadoEm { get; private set; }
    public Guid? EstornadoPorId { get; private set; }
    public string? MotivoEstorno { get; private set; }

    public decimal Troco => ValorRecebido - Valor;

    protected Pagamento() { }

    internal Pagamento(
        Guid caixaId,
        FormaPagamento forma,
        decimal valor,
        decimal valorRecebido,
        Guid? pessoaId,
        Guid registradoPorId
    )
    {
        CaixaId = caixaId;
        Forma = forma;
        Valor = valor;
        ValorRecebido = valorRecebido;
        PessoaId = pessoaId;
        RegistradoPorId = registradoPorId;
    }

    internal void Estornar(string motivo, Guid funcionarioId)
    {
        if (Estornado)
            throw new DomainException("O pagamento já foi estornado.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("Informe o motivo do estorno.");

        Estornado = true;
        EstornadoEm = DateTime.UtcNow;
        EstornadoPorId = funcionarioId;
        MotivoEstorno = motivo.Trim();
    }
}
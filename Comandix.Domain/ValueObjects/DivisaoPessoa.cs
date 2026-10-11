namespace Comandix.Domain.ValueObjects;

public record DivisaoPessoa(
    Guid PessoaId,
    string Nome,
    decimal Consumo,
    decimal TaxaServico,
    decimal Total
);

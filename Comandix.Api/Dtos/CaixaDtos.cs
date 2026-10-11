using System.ComponentModel.DataAnnotations;
using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record AbrirCaixaRequest(
    [Range(0, 100000, ErrorMessage = "O fundo de troco deve estar entre 0 e 100.000.")]
        decimal ValorAbertura
);

public record MovimentacaoCaixaRequest(
    [Range(0.01, 100000, ErrorMessage = "O valor deve estar entre 0,01 e 100.000.")] decimal Valor,
    [Required(ErrorMessage = "O motivo é obrigatório.")]
    [MaxLength(200, ErrorMessage = "O motivo deve ter no máximo 200 caracteres.")]
        string Motivo
);

public record FecharCaixaRequest(
    [Range(0, 1000000, ErrorMessage = "O valor contado deve estar entre 0 e 1.000.000.")]
        decimal ValorContado
);

public record MovimentacaoCaixaResponse(
    Guid Id,
    TipoMovimentacaoCaixa Tipo,
    decimal Valor,
    string Motivo,
    Guid FuncionarioId,
    DateTime RegistradaEm
);

public record VendaPorFormaResponse(FormaPagamento Forma, int Quantidade, decimal Total);

public record CaixaResponse(
    Guid Id,
    StatusCaixa Status,
    Guid AbertoPorId,
    DateTime AbertoEm,
    DateTime? FechadoEm,
    decimal ValorAbertura,
    decimal TotalSuprimentos,
    decimal TotalSangrias,
    IEnumerable<VendaPorFormaResponse> Vendas,
    decimal TotalVendas,
    decimal DinheiroEsperado,
    decimal? ValorContadoDinheiro,
    decimal? Diferenca,
    IEnumerable<MovimentacaoCaixaResponse> Movimentacoes
);
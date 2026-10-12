using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record NotaFiscalItemResponse(
    string Descricao,
    int Quantidade,
    decimal ValorUnitario,
    decimal ValorTotal
);

public record NotaFiscalPagamentoResponse(FormaPagamento Forma, decimal Valor);

public record NotaFiscalResponse(
    string? Aviso,
    Guid Id,
    Guid ComandaId,
    int Serie,
    int Numero,
    string ChaveAcesso,
    string Protocolo,
    DateTime EmitidaEm,
    string EmitenteRazaoSocial,
    string EmitenteCnpj,
    IEnumerable<NotaFiscalItemResponse> Itens,
    decimal ValorProdutos,
    decimal TaxaServico,
    decimal ValorTotal,
    IEnumerable<NotaFiscalPagamentoResponse> Pagamentos,
    decimal Troco
);

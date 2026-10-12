using System.ComponentModel.DataAnnotations;
using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record AbrirComandaRequest(Guid MesaId, Guid? GarcomId = null);

public record AdicionarItemRequest(
    Guid ProdutoId,
    [Range(1, 100, ErrorMessage = "A quantidade deve estar entre 1 e 100.")] int Quantidade,
    [MaxLength(250, ErrorMessage = "A observação deve ter no máximo 250 caracteres.")]
        string? Observacao
);

public record DefinirPessoasRequest(
    [Range(1, 50, ErrorMessage = "A quantidade de pessoas deve estar entre 1 e 50.")] int Quantidade
);

public record RenomearPessoaRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(60, ErrorMessage = "O nome deve ter no máximo 60 caracteres.")]
        string Nome
);

public record AtribuirItemRequest(
    [Required(ErrorMessage = "Informe a lista de pessoas (vazia para dividir entre todos).")]
        List<Guid> PessoaIds
);

public record TaxaServicoRequest(bool Cobrar);

public record TrocarMesaRequest(Guid MesaId);

public record RegistrarPagamentoRequest(
    [EnumDataType(typeof(FormaPagamento), ErrorMessage = "Forma de pagamento inválida.")]
        FormaPagamento Forma,
    [Range(0.01, 100000, ErrorMessage = "O valor deve estar entre 0,01 e 100.000.")] decimal Valor,
    [Range(0.01, 100000, ErrorMessage = "O valor recebido deve estar entre 0,01 e 100.000.")]
        decimal? ValorRecebido = null,
    Guid? PessoaId = null
);

public record EstornarPagamentoRequest(
    [Required(ErrorMessage = "O motivo do estorno é obrigatório.")]
    [MaxLength(200, ErrorMessage = "O motivo deve ter no máximo 200 caracteres.")]
        string Motivo
);

public record ItemComandaResponse(
    Guid Id,
    Guid ProdutoId,
    string NomeProduto,
    decimal PrecoUnitario,
    int Quantidade,
    decimal Subtotal,
    string? Observacao,
    StatusItem Status,
    DateTime PedidoEm,
    IEnumerable<Guid> PagantesIds
);

public record DivisaoPessoaResponse(
    Guid PessoaId,
    string Nome,
    decimal Consumo,
    decimal TaxaServico,
    decimal Total,
    decimal Pago,
    decimal Saldo
);

public record PagamentoResponse(
    Guid Id,
    FormaPagamento Forma,
    decimal Valor,
    decimal ValorRecebido,
    decimal Troco,
    Guid? PessoaId,
    Guid CaixaId,
    DateTime RegistradoEm,
    bool Estornado,
    string? MotivoEstorno
);

public record ComandaResponse(
    Guid Id,
    Guid MesaId,
    int MesaNumero,
    Guid GarcomId,
    string Garcom,
    StatusComanda Status,
    DateTime AbertaEm,
    DateTime? FechadaEm,
    bool CobrarTaxaServico,
    decimal PercentualTaxaServico,
    decimal Consumo,
    decimal TaxaServico,
    decimal Total,
    decimal TotalPago,
    decimal Saldo,
    IEnumerable<ItemComandaResponse> Itens,
    IEnumerable<DivisaoPessoaResponse> Divisao,
    IEnumerable<PagamentoResponse> Pagamentos
);

public record ComandaResumoResponse(
    Guid Id,
    Guid MesaId,
    int MesaNumero,
    Guid GarcomId,
    string Garcom,
    StatusComanda Status,
    DateTime AbertaEm,
    DateTime? FechadaEm,
    decimal Consumo,
    decimal TaxaServico,
    decimal Total,
    decimal Saldo,
    int ItensProntos
);

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
    decimal Total
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
    IEnumerable<ItemComandaResponse> Itens,
    IEnumerable<DivisaoPessoaResponse> Divisao
);

public record ComandaResumoResponse(
    Guid Id,
    int MesaNumero,
    string Garcom,
    StatusComanda Status,
    DateTime AbertaEm,
    DateTime? FechadaEm,
    decimal Consumo,
    decimal TaxaServico,
    decimal Total
);

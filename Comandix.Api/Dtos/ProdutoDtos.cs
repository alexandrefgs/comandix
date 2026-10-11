using System.ComponentModel.DataAnnotations;

namespace Comandix.Api.Dtos;

public record ProdutoRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        string Nome,
    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres.")]
        string? Descricao,
    [Range(0.01, 99999999.99, ErrorMessage = "O preço deve ser maior que zero.")] decimal Preco,
    Guid CategoriaId
);

public record ProdutoResponse(
    Guid Id,
    string Nome,
    string? Descricao,
    decimal Preco,
    bool Disponivel,
    Guid CategoriaId,
    string Categoria
);

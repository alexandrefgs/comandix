using System.ComponentModel.DataAnnotations;

namespace Comandix.Api.Dtos;

public record CategoriaRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        string Nome
);

public record CategoriaResponse(Guid Id, string Nome, bool Ativa);

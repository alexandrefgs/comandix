using System.ComponentModel.DataAnnotations;
using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record FuncionarioRequest(
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
        string Nome,
    [EnumDataType(typeof(Cargo), ErrorMessage = "Cargo inválido.")] Cargo Cargo
);

public record FuncionarioResponse(Guid Id, string Nome, Cargo Cargo, bool Ativo);

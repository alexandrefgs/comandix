using System.ComponentModel.DataAnnotations;
using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record LoginRequest(
    [Required(ErrorMessage = "O usuário é obrigatório.")] string Usuario,
    [Required(ErrorMessage = "A senha é obrigatória.")] string Senha
);

public record LoginResponse(
    string Token,
    DateTime ExpiraEm,
    Guid FuncionarioId,
    string Nome,
    Cargo Cargo
);

public record CredenciaisRequest(
    [Required(ErrorMessage = "O usuário é obrigatório.")]
    [MaxLength(50, ErrorMessage = "O usuário deve ter no máximo 50 caracteres.")]
    [RegularExpression(
        "^[a-zA-Z0-9._-]+$",
        ErrorMessage = "O usuário deve conter apenas letras, números, ponto, hífen ou sublinhado."
    )]
        string Usuario,
    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
        string Senha
);
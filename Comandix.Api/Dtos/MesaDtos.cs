using System.ComponentModel.DataAnnotations;
using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record MesaRequest(
    [Range(1, 9999, ErrorMessage = "O número da mesa deve estar entre 1 e 9999.")] int Numero,
    [Range(1, 50, ErrorMessage = "A capacidade deve estar entre 1 e 50.")] int Capacidade
);

public record MesaResponse(Guid Id, int Numero, int Capacidade, bool Ativa, StatusMesa Status);

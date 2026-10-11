using Comandix.Domain.Enums;

namespace Comandix.Api.Dtos;

public record FilaCozinhaItemResponse(
    Guid ComandaId,
    int MesaNumero,
    Guid ItemId,
    string NomeProduto,
    int Quantidade,
    string? Observacao,
    StatusItem Status,
    DateTime PedidoEm);
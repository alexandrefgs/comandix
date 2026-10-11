namespace Comandix.Api.Dtos;

public record TaxaServicoGarcomResponse(
    Guid GarcomId,
    string Garcom,
    int Comandas,
    decimal TotalTaxaServico
);

public record RelatorioTaxaServicoResponse(
    int Ano,
    int Mes,
    decimal Total,
    IEnumerable<TaxaServicoGarcomResponse> Garcons
);

using Comandix.Api.Dtos;
using Comandix.Domain.Entities;

namespace Comandix.Api.Mappings;

public static class ComandaMappings
{
    public static ComandaResponse ToResponse(this Comanda comanda) =>
        new(
            comanda.Id,
            comanda.MesaId,
            comanda.Mesa.Numero,
            comanda.GarcomId,
            comanda.Garcom.Nome,
            comanda.Status,
            comanda.CriadoEm,
            comanda.FechadaEm,
            comanda.CobrarTaxaServico,
            comanda.PercentualTaxaServico,
            comanda.Consumo,
            comanda.CalcularTaxaServico(),
            comanda.TotalComTaxa,
            comanda.Itens
                .OrderBy(i => i.CriadoEm)
                .Select(i => new ItemComandaResponse(
                    i.Id,
                    i.ProdutoId,
                    i.NomeProduto,
                    i.PrecoUnitario,
                    i.Quantidade,
                    i.Subtotal,
                    i.Observacao,
                    i.Status,
                    i.CriadoEm,
                    i.Pagantes.Select(p => p.Id))),
            comanda.CalcularDivisao()
                .Select(d => new DivisaoPessoaResponse(d.PessoaId, d.Nome, d.Consumo, d.TaxaServico, d.Total)));

    public static ComandaResumoResponse ToResumoResponse(this Comanda comanda) =>
        new(
            comanda.Id,
            comanda.Mesa.Numero,
            comanda.Garcom.Nome,
            comanda.Status,
            comanda.CriadoEm,
            comanda.FechadaEm,
            comanda.Consumo,
            comanda.CalcularTaxaServico(),
            comanda.TotalComTaxa);
}
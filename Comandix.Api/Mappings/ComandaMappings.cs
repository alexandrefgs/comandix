using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;

namespace Comandix.Api.Mappings;

public static class ComandaMappings
{
    public static ComandaResponse ToResponse(this Comanda comanda)
    {
        var pagamentosAtivos = comanda.Pagamentos.Where(p => !p.Estornado).ToList();

        return new ComandaResponse(
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
            comanda.TotalPago,
            comanda.Saldo,
            comanda
                .Itens.OrderBy(i => i.CriadoEm)
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
                    i.Pagantes.Select(p => p.Id)
                )),
            comanda
                .CalcularDivisao()
                .Select(d =>
                {
                    var pago = pagamentosAtivos
                        .Where(p => p.PessoaId == d.PessoaId)
                        .Sum(p => p.Valor);

                    return new DivisaoPessoaResponse(
                        d.PessoaId,
                        d.Nome,
                        d.Consumo,
                        d.TaxaServico,
                        d.Total,
                        pago,
                        d.Total - pago
                    );
                }),
            comanda
                .Pagamentos.OrderBy(p => p.CriadoEm)
                .Select(p => new PagamentoResponse(
                    p.Id,
                    p.Forma,
                    p.Valor,
                    p.ValorRecebido,
                    p.Troco,
                    p.PessoaId,
                    p.CaixaId,
                    p.CriadoEm,
                    p.Estornado,
                    p.MotivoEstorno
                ))
        );
    }

    public static ComandaResumoResponse ToResumoResponse(this Comanda comanda) =>
    new(
        comanda.Id,
        comanda.MesaId,
        comanda.Mesa.Numero,
        comanda.GarcomId,
        comanda.Garcom.Nome,
        comanda.Status,
        comanda.CriadoEm,
        comanda.FechadaEm,
        comanda.Consumo,
        comanda.CalcularTaxaServico(),
        comanda.TotalComTaxa,
        comanda.Saldo,
        comanda.Itens.Count(i => i.Status == StatusItem.Pronto)
    );
}

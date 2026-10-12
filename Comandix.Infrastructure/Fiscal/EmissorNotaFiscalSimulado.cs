using System.Security.Cryptography;
using Comandix.Domain.Entities;
using Comandix.Domain.Fiscal;
using Comandix.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Comandix.Infrastructure.Fiscal;

public class EmissorNotaFiscalSimulado(
    ComandixDbContext context,
    IOptions<NotaFiscalOptions> options
) : IEmissorNotaFiscal
{
    private static readonly TimeZoneInfo FusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Sao_Paulo"
    );

    public async Task<NotaFiscal> EmitirAsync(
        Comanda comanda,
        CancellationToken cancellationToken = default
    )
    {
        var config = options.Value;

        var ultimoNumero =
            await context
                .NotasFiscais.Where(n => n.Serie == config.Serie)
                .MaxAsync(n => (int?)n.Numero, cancellationToken)
            ?? 0;

        var numero = ultimoNumero + 1;
        var emissao = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

        var chave = ChaveAcessoNfce.Gerar(
            config.CodigoUf,
            emissao,
            config.CnpjEmitente,
            config.Serie,
            numero,
            RandomNumberGenerator.GetInt32(10_000_000, 100_000_000)
        );

        var nota = new NotaFiscal(
            comanda.Id,
            config.Serie,
            numero,
            chave,
            $"SIMULACAO-{numero:D9}",
            comanda.TotalComTaxa,
            simulada: true
        );

        context.NotasFiscais.Add(nota);
        return nota;
    }
}

using System.ComponentModel.DataAnnotations;
using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/relatorios")]
public class RelatoriosController(ComandixDbContext context) : ApiControllerBase
{
    private static readonly TimeZoneInfo FusoBrasilia = TimeZoneInfo.FindSystemTimeZoneById(
        "America/Sao_Paulo"
    );

    [HttpGet("taxa-servico")]
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RelatorioTaxaServicoResponse>> TaxaServico(
        [FromQuery, Range(2000, 2100, ErrorMessage = "Ano inválido.")] int ano,
        [FromQuery, Range(1, 12, ErrorMessage = "O mês deve estar entre 1 e 12.")] int mes,
        [FromQuery] Guid? garcomId = null
    )
    {
        if (UsuarioEhGarcom)
            garcomId = FuncionarioLogadoId;

        var inicioLocal = new DateTime(ano, mes, 1);
        var inicio = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, FusoBrasilia);
        var fim = TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddMonths(1), FusoBrasilia);

        var query = context
            .Comandas.AsNoTracking()
            .Where(c =>
                c.Status == StatusComanda.Fechada && c.FechadaEm >= inicio && c.FechadaEm < fim
            );

        if (garcomId.HasValue)
            query = query.Where(c => c.GarcomId == garcomId.Value);

        var garcons = await query
            .GroupBy(c => new { c.GarcomId, c.Garcom.Nome })
            .Select(g => new TaxaServicoGarcomResponse(
                g.Key.GarcomId,
                g.Key.Nome,
                g.Count(),
                g.Sum(c => c.ValorTaxaServico)
            ))
            .ToListAsync();

        var ordenados = garcons.OrderByDescending(g => g.TotalTaxaServico).ToList();

        return Ok(
            new RelatorioTaxaServicoResponse(
                ano,
                mes,
                ordenados.Sum(g => g.TotalTaxaServico),
                ordenados
            )
        );
    }
}
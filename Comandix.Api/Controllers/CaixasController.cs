using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/caixas")]
public class CaixasController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet("atual")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaixaResponse>> ObterAtual()
    {
        var caixa = await CaixasComMovimentacoes()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Status == StatusCaixa.Aberto);

        return caixa is null ? NotFound() : Ok(await MontarResposta(caixa));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Perfis.Financeiro)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaixaResponse>> ObterPorId(Guid id)
    {
        var caixa = await CaixasComMovimentacoes()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        return caixa is null ? NotFound() : Ok(await MontarResposta(caixa));
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Financeiro)]
    [ProducesResponseType<CaixaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CaixaResponse>> Abrir(AbrirCaixaRequest request)
    {
        if (await context.Caixas.AnyAsync(c => c.Status == StatusCaixa.Aberto))
            return Conflito("Já existe um caixa aberto.");

        var caixa = new Caixa(FuncionarioLogadoId, request.ValorAbertura);
        context.Caixas.Add(caixa);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflito("Já existe um caixa aberto.");
        }

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = caixa.Id },
            await MontarResposta(caixa)
        );
    }

    [HttpPost("{id:guid}/suprimento")]
    [Authorize(Roles = Perfis.Financeiro)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaixaResponse>> RegistrarSuprimento(
        Guid id,
        MovimentacaoCaixaRequest request
    )
    {
        var caixa = await CaixasComMovimentacoes().FirstOrDefaultAsync(c => c.Id == id);
        if (caixa is null)
            return NotFound();

        caixa.RegistrarSuprimento(request.Valor, request.Motivo, FuncionarioLogadoId);
        await context.SaveChangesAsync();

        return Ok(await MontarResposta(caixa));
    }

    [HttpPost("{id:guid}/sangria")]
    [Authorize(Roles = Perfis.Financeiro)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaixaResponse>> RegistrarSangria(
        Guid id,
        MovimentacaoCaixaRequest request
    )
    {
        var caixa = await CaixasComMovimentacoes().FirstOrDefaultAsync(c => c.Id == id);
        if (caixa is null)
            return NotFound();

        caixa.RegistrarSangria(request.Valor, request.Motivo, FuncionarioLogadoId);
        await context.SaveChangesAsync();

        return Ok(await MontarResposta(caixa));
    }

    [HttpPost("{id:guid}/fechar")]
    [Authorize(Roles = Perfis.Financeiro)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CaixaResponse>> Fechar(Guid id, FecharCaixaRequest request)
    {
        var caixa = await CaixasComMovimentacoes().FirstOrDefaultAsync(c => c.Id == id);
        if (caixa is null)
            return NotFound();

        var vendasDinheiro = await context
            .Set<Pagamento>()
            .Where(p => p.CaixaId == id && p.Forma == FormaPagamento.Dinheiro && !p.Estornado)
            .SumAsync(p => p.Valor);

        caixa.Fechar(vendasDinheiro, request.ValorContado, FuncionarioLogadoId);
        await context.SaveChangesAsync();

        return Ok(await MontarResposta(caixa));
    }

    private IQueryable<Caixa> CaixasComMovimentacoes() =>
        context.Caixas.Include(c => c.Movimentacoes);

    private async Task<CaixaResponse> MontarResposta(Caixa caixa)
    {
        var vendas = await context
            .Set<Pagamento>()
            .AsNoTracking()
            .Where(p => p.CaixaId == caixa.Id && !p.Estornado)
            .GroupBy(p => p.Forma)
            .Select(g => new
            {
                Forma = g.Key,
                Quantidade = g.Count(),
                Total = g.Sum(p => p.Valor),
            })
            .ToListAsync();

        var vendasPorForma = Enum.GetValues<FormaPagamento>()
            .Select(forma =>
            {
                var venda = vendas.FirstOrDefault(v => v.Forma == forma);
                return new VendaPorFormaResponse(forma, venda?.Quantidade ?? 0, venda?.Total ?? 0m);
            })
            .ToList();

        var vendasDinheiro = vendasPorForma.Single(v => v.Forma == FormaPagamento.Dinheiro).Total;

        return new CaixaResponse(
            caixa.Id,
            caixa.Status,
            caixa.AbertoPorId,
            caixa.CriadoEm,
            caixa.FechadoEm,
            caixa.ValorAbertura,
            caixa.TotalSuprimentos,
            caixa.TotalSangrias,
            vendasPorForma,
            vendasPorForma.Sum(v => v.Total),
            caixa.ValorEsperadoDinheiro ?? caixa.CalcularDinheiroEsperado(vendasDinheiro),
            caixa.ValorContadoDinheiro,
            caixa.Diferenca,
            caixa
                .Movimentacoes.OrderBy(m => m.CriadoEm)
                .Select(m => new MovimentacaoCaixaResponse(
                    m.Id,
                    m.Tipo,
                    m.Valor,
                    m.Motivo,
                    m.FuncionarioId,
                    m.CriadoEm
                ))
        );
    }
}

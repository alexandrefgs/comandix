using Comandix.Api.Dtos;
using Comandix.Api.Mappings;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[ApiController]
[Route("api/comandas")]
public class ComandasController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComandaResumoResponse>>> Listar(
        [FromQuery] StatusComanda status = StatusComanda.Aberta,
        [FromQuery] Guid? mesaId = null,
        [FromQuery] Guid? garcomId = null
    )
    {
        var query = ComandasCompletas().AsNoTracking().Where(c => c.Status == status);

        if (mesaId.HasValue)
            query = query.Where(c => c.MesaId == mesaId.Value);

        if (garcomId.HasValue)
            query = query.Where(c => c.GarcomId == garcomId.Value);

        var comandas = await query.OrderByDescending(c => c.CriadoEm).ToListAsync();

        return Ok(comandas.Select(c => c.ToResumoResponse()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComandaResponse>> ObterPorId(Guid id)
    {
        var comanda = await ComandasCompletas().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return comanda is null ? NotFound() : Ok(comanda.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<ComandaResponse>> Abrir(AbrirComandaRequest request)
    {
        var mesa = await context.Mesas.FindAsync(request.MesaId);
        if (mesa is null)
            return RequisicaoInvalida("Mesa não encontrada.");

        var garcom = await context.Funcionarios.FindAsync(request.GarcomId);
        if (garcom is null)
            return RequisicaoInvalida("Garçom não encontrado.");

        var comanda = new Comanda(mesa, garcom);
        context.Comandas.Add(comanda);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflito("Já existe uma comanda aberta para esta mesa.");
        }

        return CreatedAtAction(nameof(ObterPorId), new { id = comanda.Id }, comanda.ToResponse());
    }

    [HttpPost("{id:guid}/itens")]
    public async Task<ActionResult<ComandaResponse>> AdicionarItem(
        Guid id,
        AdicionarItemRequest request
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        var produto = await context.Produtos.FindAsync(request.ProdutoId);
        if (produto is null)
            return RequisicaoInvalida("Produto não encontrado.");

        comanda.AdicionarItem(produto, request.Quantidade, request.Observacao?.Trim());
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPatch("{id:guid}/itens/{itemId:guid}/cancelar")]
    public async Task<ActionResult<ComandaResponse>> CancelarItem(Guid id, Guid itemId)
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.CancelarItem(itemId);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPatch("{id:guid}/itens/{itemId:guid}/avancar")]
    public async Task<ActionResult<ComandaResponse>> AvancarItem(Guid id, Guid itemId)
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.AvancarStatusItem(itemId);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPut("{id:guid}/itens/{itemId:guid}/pagantes")]
    public async Task<ActionResult<ComandaResponse>> AtribuirItem(
        Guid id,
        Guid itemId,
        AtribuirItemRequest request
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.AtribuirItem(itemId, request.PessoaIds);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPut("{id:guid}/pessoas")]
    public async Task<ActionResult<ComandaResponse>> DefinirPessoas(
        Guid id,
        DefinirPessoasRequest request
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.DefinirQuantidadePessoas(request.Quantidade);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPatch("{id:guid}/pessoas/{pessoaId:guid}")]
    public async Task<ActionResult<ComandaResponse>> RenomearPessoa(
        Guid id,
        Guid pessoaId,
        RenomearPessoaRequest request
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.RenomearPessoa(pessoaId, request.Nome.Trim());
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPatch("{id:guid}/taxa-servico")]
    public async Task<ActionResult<ComandaResponse>> DefinirTaxaServico(
        Guid id,
        TaxaServicoRequest request
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.DefinirCobrancaTaxaServico(request.Cobrar);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPost("{id:guid}/fechar")]
    public async Task<ActionResult<ComandaResponse>> Fechar(Guid id)
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.Fechar();
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<ComandaResponse>> Cancelar(Guid id)
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.Cancelar();
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    private IQueryable<Comanda> ComandasCompletas() =>
        context
            .Comandas.Include(c => c.Mesa)
            .Include(c => c.Garcom)
            .Include(c => c.Pessoas)
            .Include(c => c.Itens)
                .ThenInclude(i => i.Pagantes)
            .AsSplitQuery();

    private Task<Comanda?> ObterComanda(Guid id) =>
        ComandasCompletas().FirstOrDefaultAsync(c => c.Id == id);
}

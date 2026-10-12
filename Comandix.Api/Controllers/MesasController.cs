using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/mesas")]
public class MesasController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MesaResponse>>> Listar(
        [FromQuery] StatusMesa? status,
        [FromQuery] bool apenasAtivas = false
    )
    {
        var query = context.Mesas.AsNoTracking();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (apenasAtivas)
            query = query.Where(m => m.Ativa);

        var mesas = await query
            .OrderBy(m => m.Numero)
            .Select(m => new MesaResponse(m.Id, m.Numero, m.Capacidade, m.Ativa, m.Status))
            .ToListAsync();

        return Ok(mesas);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MesaResponse>> ObterPorId(Guid id)
    {
        var mesa = await context
            .Mesas.AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MesaResponse(m.Id, m.Numero, m.Capacidade, m.Ativa, m.Status))
            .FirstOrDefaultAsync();

        return mesa is null ? NotFound() : Ok(mesa);
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType<MesaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MesaResponse>> Criar(MesaRequest request)
    {
        if (await context.Mesas.AnyAsync(m => m.Numero == request.Numero))
            return Conflito($"Já existe a mesa {request.Numero}.");

        var mesa = new Mesa(request.Numero, request.Capacidade);
        context.Mesas.Add(mesa);
        await context.SaveChangesAsync();

        var response = new MesaResponse(
            mesa.Id,
            mesa.Numero,
            mesa.Capacidade,
            mesa.Ativa,
            mesa.Status
        );
        return CreatedAtAction(nameof(ObterPorId), new { id = mesa.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(Guid id, MesaRequest request)
    {
        var mesa = await context.Mesas.FindAsync(id);
        if (mesa is null)
            return NotFound();

        if (await context.Mesas.AnyAsync(m => m.Numero == request.Numero && m.Id != id))
            return Conflito($"Já existe a mesa {request.Numero}.");

        mesa.Atualizar(request.Numero, request.Capacidade);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ativar(Guid id)
    {
        var mesa = await context.Mesas.FindAsync(id);
        if (mesa is null)
            return NotFound();

        mesa.Ativar();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/desativar")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var mesa = await context.Mesas.FindAsync(id);
        if (mesa is null)
            return NotFound();

        mesa.Desativar();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(Guid id)
    {
        var mesa = await context.Mesas.FindAsync(id);
        if (mesa is null)
            return NotFound();

        if (await context.Comandas.AnyAsync(c => c.MesaId == id))
            return Conflito("A mesa já tem comandas registradas. Desative-a em vez de remover.");

        context.Mesas.Remove(mesa);
        await context.SaveChangesAsync();

        return NoContent();
    }
}

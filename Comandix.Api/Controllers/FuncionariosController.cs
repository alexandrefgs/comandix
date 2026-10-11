using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[ApiController]
[Route("api/funcionarios")]
public class FuncionariosController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FuncionarioResponse>>> Listar(
        [FromQuery] Cargo? cargo,
        [FromQuery] bool apenasAtivos = false
    )
    {
        var query = context.Funcionarios.AsNoTracking();

        if (cargo.HasValue)
            query = query.Where(f => f.Cargo == cargo.Value);

        if (apenasAtivos)
            query = query.Where(f => f.Ativo);

        var funcionarios = await query
            .OrderBy(f => f.Nome)
            .Select(f => new FuncionarioResponse(f.Id, f.Nome, f.Cargo, f.Ativo))
            .ToListAsync();

        return Ok(funcionarios);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FuncionarioResponse>> ObterPorId(Guid id)
    {
        var funcionario = await context
            .Funcionarios.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FuncionarioResponse(f.Id, f.Nome, f.Cargo, f.Ativo))
            .FirstOrDefaultAsync();

        return funcionario is null ? NotFound() : Ok(funcionario);
    }

    [HttpPost]
    public async Task<ActionResult<FuncionarioResponse>> Criar(FuncionarioRequest request)
    {
        var funcionario = new Funcionario(request.Nome.Trim(), request.Cargo);
        context.Funcionarios.Add(funcionario);
        await context.SaveChangesAsync();

        var response = new FuncionarioResponse(
            funcionario.Id,
            funcionario.Nome,
            funcionario.Cargo,
            funcionario.Ativo
        );
        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, FuncionarioRequest request)
    {
        var funcionario = await context.Funcionarios.FindAsync(id);
        if (funcionario is null)
            return NotFound();

        funcionario.Atualizar(request.Nome.Trim(), request.Cargo);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id)
    {
        var funcionario = await context.Funcionarios.FindAsync(id);
        if (funcionario is null)
            return NotFound();

        funcionario.Ativar();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var funcionario = await context.Funcionarios.FindAsync(id);
        if (funcionario is null)
            return NotFound();

        if (
            await context.Comandas.AnyAsync(c =>
                c.GarcomId == id && c.Status == StatusComanda.Aberta
            )
        )
            return Conflito("O funcionário tem comandas abertas. Feche-as antes de desativá-lo.");

        funcionario.Desativar();
        await context.SaveChangesAsync();

        return NoContent();
    }
}

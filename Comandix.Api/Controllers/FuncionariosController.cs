using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/funcionarios")]
public class FuncionariosController(
    ComandixDbContext context,
    IPasswordHasher<Funcionario> hasher
) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType<FuncionarioResponse>(StatusCodes.Status201Created)]
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
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    [HttpPut("{id:guid}/credenciais")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DefinirCredenciais(Guid id, CredenciaisRequest request)
    {
        var funcionario = await context.Funcionarios.FindAsync(id);
        if (funcionario is null)
            return NotFound();

        var usuario = request.Usuario.Trim().ToLowerInvariant();

        if (await context.Funcionarios.AnyAsync(f => f.Usuario == usuario && f.Id != id))
            return Conflito("Já existe um funcionário com esse usuário.");

        funcionario.DefinirCredenciais(usuario, hasher.HashPassword(funcionario, request.Senha));
        await context.SaveChangesAsync();

        return NoContent();
    }
    [HttpPost("{id:guid}/codigo-acesso")]
[Authorize(Roles = Perfis.Gerente)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<CodigoAcessoResponse>> GerarCodigoAcesso(Guid id)
{
    var funcionario = await context.Funcionarios.FindAsync(id);
    if (funcionario is null)
        return NotFound();

    var (codigo, hash) = CodigoAcesso.Gerar();
    funcionario.DefinirCodigoAcesso(hash);
    await context.SaveChangesAsync();

    return Ok(new CodigoAcessoResponse(codigo));
}

[HttpPost("{id:guid}/pin/resetar")]
[Authorize(Roles = Perfis.Gerente)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> ResetarPin(Guid id)
{
    var funcionario = await context.Funcionarios.FindAsync(id);
    if (funcionario is null)
        return NotFound();

    funcionario.ResetarPin();
    await context.SaveChangesAsync();

    return NoContent();
}

[HttpPost("{id:guid}/desbloquear")]
[Authorize(Roles = Perfis.Gerente)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Desbloquear(Guid id)
{
    var funcionario = await context.Funcionarios.FindAsync(id);
    if (funcionario is null)
        return NotFound();

    funcionario.Desbloquear();
    await context.SaveChangesAsync();

    return NoContent();
}
}
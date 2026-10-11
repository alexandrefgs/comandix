using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController(ComandixDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> Listar()
    {
        var categorias = await context
            .Categorias.AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Ativa))
            .ToListAsync();

        return Ok(categorias);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoriaResponse>> ObterPorId(Guid id)
    {
        var categoria = await context
            .Categorias.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Ativa))
            .FirstOrDefaultAsync();

        return categoria is null ? NotFound() : Ok(categoria);
    }

    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar(CategoriaRequest request)
    {
        var nome = request.Nome.Trim();

        if (await context.Categorias.AnyAsync(c => c.Nome == nome))
            return Conflict(new { mensagem = "Já existe uma categoria com esse nome." });

        var categoria = new Categoria(nome);
        context.Categorias.Add(categoria);
        await context.SaveChangesAsync();

        var response = new CategoriaResponse(categoria.Id, categoria.Nome, categoria.Ativa);
        return CreatedAtAction(nameof(ObterPorId), new { id = categoria.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, CategoriaRequest request)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        var nome = request.Nome.Trim();

        if (await context.Categorias.AnyAsync(c => c.Nome == nome && c.Id != id))
            return Conflict(new { mensagem = "Já existe uma categoria com esse nome." });

        categoria.Atualizar(nome);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/ativar")]
    public async Task<IActionResult> Ativar(Guid id)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        categoria.Ativar();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/desativar")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        categoria.Desativar();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remover(Guid id)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria is null)
            return NotFound();

        if (await context.Produtos.AnyAsync(p => p.CategoriaId == id))
            return Conflict(
                new
                {
                    mensagem = "Não é possível remover uma categoria com produtos. Desative-a ou mova os produtos.",
                }
            );

        context.Categorias.Remove(categoria);
        await context.SaveChangesAsync();

        return NoContent();
    }
}

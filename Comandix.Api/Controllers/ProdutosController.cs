using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/produtos")]
public class ProdutosController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> Listar(
        [FromQuery] Guid? categoriaId,
        [FromQuery] bool apenasDisponiveis = false
    )
    {
        var query = context.Produtos.AsNoTracking();

        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);

        if (apenasDisponiveis)
            query = query.Where(p => p.Disponivel);

        var produtos = await query
            .OrderBy(p => p.Categoria.Nome)
            .ThenBy(p => p.Nome)
            .Select(p => new ProdutoResponse(
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.Disponivel,
                p.CategoriaId,
                p.Categoria.Nome
            ))
            .ToListAsync();

        return Ok(produtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoResponse>> ObterPorId(Guid id)
    {
        var produto = await BuscarResponse(id);
        return produto is null ? NotFound() : Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType<ProdutoResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProdutoResponse>> Criar(ProdutoRequest request)
    {
        if (!await context.Categorias.AnyAsync(c => c.Id == request.CategoriaId))
            return RequisicaoInvalida("Categoria não encontrada.");

        var produto = new Produto(
            request.Nome.Trim(),
            request.Descricao?.Trim(),
            request.Preco,
            request.CategoriaId
        );
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();

        var response = await BuscarResponse(produto.Id);
        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, ProdutoRequest request)
    {
        var produto = await context.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound();

        if (!await context.Categorias.AnyAsync(c => c.Id == request.CategoriaId))
            return RequisicaoInvalida("Categoria não encontrada.");

        produto.Atualizar(
            request.Nome.Trim(),
            request.Descricao?.Trim(),
            request.Preco,
            request.CategoriaId
        );
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/disponivel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarDisponivel(Guid id)
    {
        var produto = await context.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound();

        produto.MarcarDisponivel();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:guid}/indisponivel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarcarIndisponivel(Guid id)
    {
        var produto = await context.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound();

        produto.MarcarIndisponivel();
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(Guid id)
    {
        var produto = await context.Produtos.FindAsync(id);
        if (produto is null)
            return NotFound();

        if (await context.Set<ItemComanda>().AnyAsync(i => i.ProdutoId == id))
            return Conflito(
                "O produto já foi pedido em comandas. Marque-o como indisponível em vez de remover."
            );

        context.Produtos.Remove(produto);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private Task<ProdutoResponse?> BuscarResponse(Guid id) =>
        context
            .Produtos.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProdutoResponse(
                p.Id,
                p.Nome,
                p.Descricao,
                p.Preco,
                p.Disponivel,
                p.CategoriaId,
                p.Categoria.Nome
            ))
            .FirstOrDefaultAsync();
}

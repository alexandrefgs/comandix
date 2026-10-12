using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/cozinha")]
public class CozinhaController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet("fila")]
    [Authorize(Roles = Perfis.Preparo)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FilaCozinhaItemResponse>>> Fila()
    {
        var fila = await context
            .Comandas.AsNoTracking()
            .Where(c => c.Status == StatusComanda.Aberta)
            .SelectMany(c => c.Itens, (c, i) => new { Comanda = c, Item = i })
            .Where(x =>
                x.Item.Status == StatusItem.Pendente
                || x.Item.Status == StatusItem.EmPreparo
                || x.Item.Status == StatusItem.Pronto
            )
            .OrderBy(x => x.Item.CriadoEm)
            .Select(x => new FilaCozinhaItemResponse(
                x.Comanda.Id,
                x.Comanda.Mesa.Numero,
                x.Item.Id,
                x.Item.NomeProduto,
                x.Item.Quantidade,
                x.Item.Observacao,
                x.Item.Status,
                x.Item.CriadoEm
            ))
            .ToListAsync();

        return Ok(fila);
    }
}

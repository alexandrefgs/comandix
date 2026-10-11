using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Api.Mappings;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/comandas")]
public class ComandasController(ComandixDbContext context) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComandaResponse>> ObterPorId(Guid id)
    {
        var comanda = await ComandasCompletas().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        return comanda is null ? NotFound() : Ok(comanda.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType<ComandaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ComandaResponse>> Abrir(AbrirComandaRequest request)
    {
        var garcomId = UsuarioEhGarcom ? FuncionarioLogadoId : request.GarcomId;
        if (garcomId is null)
            return RequisicaoInvalida("Informe o garçom responsável pela comanda.");

        var mesa = await context.Mesas.FindAsync(request.MesaId);
        if (mesa is null)
            return RequisicaoInvalida("Mesa não encontrada.");

        var garcom = await context.Funcionarios.FindAsync(garcomId.Value);
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Preparo)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [HttpPost("{id:guid}/pagamentos")]
[Authorize(Roles = Perfis.Fechamento)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ComandaResponse>> RegistrarPagamento(
    Guid id,
    RegistrarPagamentoRequest request
)
{
    var comanda = await ObterComanda(id);
    if (comanda is null)
        return NotFound();

    var caixaId = await context
        .Caixas.Where(c => c.Status == StatusCaixa.Aberto)
        .Select(c => (Guid?)c.Id)
        .FirstOrDefaultAsync();

    if (caixaId is null)
        return RequisicaoInvalida("Não há caixa aberto. Abra o caixa antes de registrar pagamentos.");

    comanda.RegistrarPagamento(
        caixaId.Value,
        request.Forma,
        request.Valor,
        request.ValorRecebido,
        request.PessoaId,
        FuncionarioLogadoId
    );
    await context.SaveChangesAsync();

    return Ok(comanda.ToResponse());
}

[HttpPost("{id:guid}/pagamentos/{pagamentoId:guid}/estornar")]
[Authorize(Roles = Perfis.Financeiro)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<ComandaResponse>> EstornarPagamento(
    Guid id,
    Guid pagamentoId,
    EstornarPagamentoRequest request
)
{
    var comanda = await ObterComanda(id);
    if (comanda is null)
        return NotFound();

    var pagamento = comanda.Pagamentos.FirstOrDefault(p => p.Id == pagamentoId);
    if (pagamento is null)
        return NotFound();

    if (
        await context.Caixas.AnyAsync(c =>
            c.Id == pagamento.CaixaId && c.Status == StatusCaixa.Fechado
        )
    )
        return RequisicaoInvalida(
            "O pagamento pertence a um caixa já fechado e não pode ser estornado."
        );

    comanda.EstornarPagamento(pagamentoId, request.Motivo, FuncionarioLogadoId);
    await context.SaveChangesAsync();

    return Ok(comanda.ToResponse());
}

    [HttpPost("{id:guid}/fechar")]
    [Authorize(Roles = Perfis.Fechamento)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    [Authorize(Roles = Perfis.Fechamento)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        .Include(c => c.Pagamentos)
        .Include(c => c.Itens)
            .ThenInclude(i => i.Pagantes)
        .AsSplitQuery();

    private Task<Comanda?> ObterComanda(Guid id) =>
        ComandasCompletas().FirstOrDefaultAsync(c => c.Id == id);
}
using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Api.Mappings;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Domain.Fiscal;
using Comandix.Infrastructure.Data;
using Comandix.Infrastructure.Fiscal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    [HttpGet("{id:guid}/historico")]
    [Authorize(Roles = Perfis.Gerente)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EventoComandaResponse>>> ObterHistorico(Guid id)
    {
        if (!await context.Comandas.AnyAsync(c => c.Id == id))
            return NotFound();

        var eventos = await context
            .EventosComanda.AsNoTracking()
            .Where(e => e.ComandaId == id)
            .OrderBy(e => e.CriadoEm)
            .Select(e => new EventoComandaResponse(
                e.Id,
                e.Tipo,
                e.Descricao,
                e.FuncionarioId,
                e.FuncionarioNome,
                e.Valor,
                e.CriadoEm
            ))
            .ToListAsync();

        return Ok(eventos);
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
        Registrar(
            comanda,
            TipoEventoComanda.Abertura,
            garcom.Id == FuncionarioLogadoId
                ? $"Abriu a mesa {mesa.Numero}"
                : $"Abriu a mesa {mesa.Numero} para o garçom {garcom.Nome}"
        );

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

        var item = comanda.AdicionarItem(produto, request.Quantidade, request.Observacao?.Trim());
        Registrar(
            comanda,
            TipoEventoComanda.ItemAdicionado,
            item.Observacao is null
                ? $"Adicionou {Descrever(item)}"
                : $"Adicionou {Descrever(item)} ({item.Observacao})",
            item.Subtotal
        );
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
        var item = comanda.Itens.Single(i => i.Id == itemId);
        Registrar(
            comanda,
            TipoEventoComanda.ItemCancelado,
            $"Cancelou {Descrever(item)}",
            item.Subtotal
        );
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
        var item = comanda.Itens.Single(i => i.Id == itemId);
        var (tipo, descricao) = item.Status switch
        {
            StatusItem.EmPreparo => (
                TipoEventoComanda.ItemEmPreparo,
                $"Iniciou o preparo de {Descrever(item)}"
            ),
            StatusItem.Pronto => (
                TipoEventoComanda.ItemPronto,
                $"Marcou {Descrever(item)} como pronto"
            ),
            _ => (TipoEventoComanda.ItemEntregue, $"Entregou {Descrever(item)}"),
        };
        Registrar(comanda, tipo, descricao);
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
        var item = comanda.Itens.Single(i => i.Id == itemId);
        var pessoas =
            request.PessoaIds.Count == 0
                ? "todos"
                : string.Join(
                    ", ",
                    comanda
                        .Pessoas.Where(p => request.PessoaIds.Contains(p.Id))
                        .Select(p => p.Nome)
                );
        Registrar(
            comanda,
            TipoEventoComanda.ItemAtribuido,
            $"Atribuiu {Descrever(item)} a {pessoas}"
        );
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
        Registrar(
            comanda,
            TipoEventoComanda.PessoasDefinidas,
            request.Quantidade == 1
                ? "Definiu a conta para 1 pessoa"
                : $"Dividiu a conta entre {request.Quantidade} pessoas"
        );
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

        var nomeAnterior = comanda.Pessoas.FirstOrDefault(p => p.Id == pessoaId)?.Nome;
        comanda.RenomearPessoa(pessoaId, request.Nome.Trim());
        Registrar(
            comanda,
            TipoEventoComanda.PessoaRenomeada,
            $"Renomeou {nomeAnterior} para {request.Nome.Trim()}"
        );
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
        Registrar(
            comanda,
            TipoEventoComanda.TaxaServicoAlterada,
            request.Cobrar ? "Incluiu a taxa de serviço" : "Retirou a taxa de serviço"
        );
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPost("{id:guid}/trocar-mesa")]
    [Authorize(Roles = Perfis.Salao)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ComandaResponse>> TrocarMesa(Guid id, TrocarMesaRequest request)
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        var mesa = await context.Mesas.FindAsync(request.MesaId);
        if (mesa is null)
            return RequisicaoInvalida("Mesa não encontrada.");

        var mesaAnterior = comanda.Mesa.Numero;
        comanda.TrocarMesa(mesa);
        Registrar(
            comanda,
            TipoEventoComanda.MesaTrocada,
            $"Trocou da mesa {mesaAnterior} para a mesa {mesa.Numero}"
        );

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflito("A mesa escolhida acabou de ser ocupada.");
        }

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
            return RequisicaoInvalida(
                "Não há caixa aberto. Abra o caixa antes de registrar pagamentos."
            );

        var pagamento = comanda.RegistrarPagamento(
            caixaId.Value,
            request.Forma,
            request.Valor,
            request.ValorRecebido,
            request.PessoaId,
            FuncionarioLogadoId
        );
        Registrar(
            comanda,
            TipoEventoComanda.PagamentoRegistrado,
            $"Registrou pagamento em {NomeForma(pagamento.Forma)}{DaPessoa(comanda, pagamento.PessoaId)}",
            pagamento.Valor
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
        Registrar(
            comanda,
            TipoEventoComanda.PagamentoEstornado,
            $"Estornou pagamento em {NomeForma(pagamento.Forma)}{DaPessoa(comanda, pagamento.PessoaId)}. Motivo: {request.Motivo.Trim()}",
            pagamento.Valor
        );
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpPost("{id:guid}/fechar")]
    [Authorize(Roles = Perfis.Fechamento)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ComandaResponse>> Fechar(
        Guid id,
        [FromServices] IEmissorNotaFiscal emissor
    )
    {
        var comanda = await ObterComanda(id);
        if (comanda is null)
            return NotFound();

        comanda.Fechar();
        Registrar(comanda, TipoEventoComanda.Fechamento, "Fechou a comanda", comanda.TotalComTaxa);
        await emissor.EmitirAsync(comanda);
        await context.SaveChangesAsync();

        return Ok(comanda.ToResponse());
    }

    [HttpGet("{id:guid}/nota-fiscal")]
    [Authorize(Roles = Perfis.Fechamento)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotaFiscalResponse>> ObterNotaFiscal(
        Guid id,
        [FromServices] IOptions<NotaFiscalOptions> opcoes
    )
    {
        var nota = await context
            .NotasFiscais.AsNoTracking()
            .FirstOrDefaultAsync(n => n.ComandaId == id);
        if (nota is null)
            return NotFound();

        var comanda = await ComandasCompletas().AsNoTracking().FirstAsync(c => c.Id == id);
        var pagamentos = comanda.Pagamentos.Where(p => !p.Estornado).ToList();

        return Ok(
            new NotaFiscalResponse(
                nota.Simulada ? "SIMULAÇÃO - DOCUMENTO SEM VALOR FISCAL" : null,
                nota.Id,
                nota.ComandaId,
                nota.Serie,
                nota.Numero,
                nota.ChaveAcesso,
                nota.Protocolo,
                nota.CriadoEm,
                opcoes.Value.RazaoSocialEmitente,
                opcoes.Value.CnpjEmitente,
                comanda
                    .Itens.Where(i => i.Status != StatusItem.Cancelado)
                    .OrderBy(i => i.CriadoEm)
                    .Select(i => new NotaFiscalItemResponse(
                        i.NomeProduto,
                        i.Quantidade,
                        i.PrecoUnitario,
                        i.Subtotal
                    )),
                comanda.Consumo,
                comanda.ValorTaxaServico,
                nota.ValorTotal,
                pagamentos
                    .GroupBy(p => p.Forma)
                    .Select(g => new NotaFiscalPagamentoResponse(g.Key, g.Sum(p => p.Valor))),
                pagamentos.Sum(p => p.Troco)
            )
        );
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
        Registrar(comanda, TipoEventoComanda.Cancelamento, "Cancelou a comanda");
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

    private void Registrar(
        Comanda comanda,
        TipoEventoComanda tipo,
        string descricao,
        decimal? valor = null
    ) =>
        context.EventosComanda.Add(
            new EventoComanda(
                comanda.Id,
                tipo,
                descricao,
                FuncionarioLogadoId,
                User.Identity?.Name ?? "Desconhecido",
                valor
            )
        );

    private static string Descrever(ItemComanda item) => $"{item.Quantidade}× {item.NomeProduto}";

    private static string NomeForma(FormaPagamento forma) =>
        forma switch
        {
            FormaPagamento.Dinheiro => "dinheiro",
            FormaPagamento.Pix => "Pix",
            FormaPagamento.CartaoDebito => "cartão de débito",
            FormaPagamento.CartaoCredito => "cartão de crédito",
            _ => forma.ToString(),
        };

    private static string DaPessoa(Comanda comanda, Guid? pessoaId)
    {
        var nome = comanda.Pessoas.FirstOrDefault(p => p.Id == pessoaId)?.Nome;
        return nome is null ? string.Empty : $" ({nome})";
    }
}
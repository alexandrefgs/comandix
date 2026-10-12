using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class ComandasEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task FluxoCompleto_DeveDividirCobrarTaxaPagarEFecharComanda()
    {
        var caixa = await AbrirNovoCaixa(100m);
        var categoria = await CriarCategoria();
        var pizza = await CriarProduto(categoria.Id, "Pizza", 60m);
        var cerveja = await CriarProduto(categoria.Id, "Cerveja", 12m);
        var mesa = await CriarMesa();
        var garcom = await CriarGarcom();

        var comanda = await AbrirComanda(mesa.Id, garcom.Id);
        var url = $"api/comandas/{comanda.Id}";

        await PostAsync<ComandaResponse>(
            $"{url}/itens",
            new AdicionarItemRequest(pizza.Id, 1, "Sem cebola")
        );
        await PostAsync<ComandaResponse>(
            $"{url}/itens",
            new AdicionarItemRequest(cerveja.Id, 1, null)
        );
        comanda = await PutAsync<ComandaResponse>($"{url}/pessoas", new DefinirPessoasRequest(3));

        var joao = comanda.Divisao.First();
        var itemCerveja = comanda.Itens.Single(i => i.ProdutoId == cerveja.Id);

        await PatchAsync<ComandaResponse>(
            $"{url}/pessoas/{joao.PessoaId}",
            new RenomearPessoaRequest("João")
        );
        comanda = await PutAsync<ComandaResponse>(
            $"{url}/itens/{itemCerveja.Id}/pagantes",
            new AtribuirItemRequest([joao.PessoaId])
        );

        Assert.Equal(79.20m, comanda.Total);
        Assert.Equal("João", comanda.Divisao.First().Nome);
        Assert.Equal(new[] { 35.20m, 22.00m, 22.00m }, comanda.Divisao.Select(d => d.Total));

        foreach (var item in comanda.Itens)
            for (var i = 0; i < 3; i++)
                await PatchAsync<ComandaResponse>($"{url}/itens/{item.Id}/avancar");

        var pessoas = comanda.Divisao.ToList();
        await PostAsync<ComandaResponse>(
            $"{url}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 35.20m, PessoaId: pessoas[0].PessoaId)
        );
        await PostAsync<ComandaResponse>(
            $"{url}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Dinheiro, 22m, 50m, pessoas[1].PessoaId)
        );
        comanda = await PostAsync<ComandaResponse>(
            $"{url}/pagamentos",
            new RegistrarPagamentoRequest(
                FormaPagamento.CartaoCredito,
                22m,
                PessoaId: pessoas[2].PessoaId
            )
        );

        Assert.Equal(0m, comanda.Saldo);
        Assert.All(comanda.Divisao, d => Assert.Equal(0m, d.Saldo));
        Assert.Equal(28m, comanda.Pagamentos.Single(p => p.Forma == FormaPagamento.Dinheiro).Troco);

        comanda = await PostAsync<ComandaResponse>($"{url}/fechar");

        Assert.Equal(StatusComanda.Fechada, comanda.Status);
        Assert.Equal(7.20m, comanda.TaxaServico);

        var mesaAtualizada = await GetAsync<MesaResponse>($"api/mesas/{mesa.Id}");
        Assert.Equal(StatusMesa.Livre, mesaAtualizada.Status);

        var resumoCaixa = await GetAsync<CaixaResponse>($"api/caixas/{caixa.Id}");
        Assert.Equal(79.20m, resumoCaixa.TotalVendas);
        Assert.Equal(122m, resumoCaixa.DinheiroEsperado);

        var hoje = TimeZoneInfo.ConvertTime(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")
        );
        var relatorio = await GetAsync<RelatorioTaxaServicoResponse>(
            $"api/relatorios/taxa-servico?ano={hoje.Year}&mes={hoje.Month}&garcomId={garcom.Id}"
        );

        var linha = Assert.Single(relatorio.Garcons);
        Assert.Equal(1, linha.Comandas);
        Assert.Equal(7.20m, linha.TotalTaxaServico);
    }

    [Fact]
    public async Task Abrir_EmMesaOcupada_DeveRetornar400()
    {
        var mesa = await CriarMesa();
        var garcom = await CriarGarcom();
        await AbrirComanda(mesa.Id, garcom.Id);

        var response = await EnviarPost(
            "api/comandas",
            new AbrirComandaRequest(mesa.Id, garcom.Id)
        );

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal("A mesa já está ocupada.", problema.Detail);
    }

    [Fact]
    public async Task AdicionarItem_ComQuantidadeZero_DeveRetornar400()
    {
        var categoria = await CriarCategoria();
        var produto = await CriarProduto(categoria.Id, "Suco", 8m);
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);

        var response = await EnviarPost(
            $"api/comandas/{comanda.Id}/itens",
            new AdicionarItemRequest(produto.Id, 0, null)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Fechar_ComItensPendentes_DeveRetornar400()
    {
        var categoria = await CriarCategoria();
        var produto = await CriarProduto(categoria.Id, "Pizza", 60m);
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);
        await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/itens",
            new AdicionarItemRequest(produto.Id, 1, null)
        );

        var response = await EnviarPost($"api/comandas/{comanda.Id}/fechar");

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal("Ainda há itens que não foram entregues.", problema.Detail);
    }

    [Fact]
    public async Task ObterPorId_ComandaInexistente_DeveRetornar404()
    {
        var response = await Client.GetAsync($"api/comandas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

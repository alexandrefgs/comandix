using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class NotaFiscalEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task FecharComanda_DeveEmitirNotaSimulada()
    {
        await AbrirNovoCaixa();
        var comanda = await ComandaPagaEFechada();

        var nota = await GetAsync<NotaFiscalResponse>($"api/comandas/{comanda.Id}/nota-fiscal");

        Assert.Equal("SIMULAÇÃO - DOCUMENTO SEM VALOR FISCAL", nota.Aviso);
        Assert.Equal(44, nota.ChaveAcesso.Length);
        Assert.Equal("65", nota.ChaveAcesso.Substring(20, 2));
        Assert.Equal(66m, nota.ValorTotal);
        Assert.Equal(6m, nota.TaxaServico);
        var item = Assert.Single(nota.Itens);
        Assert.Equal("Pizza", item.Descricao);
        var pagamento = Assert.Single(nota.Pagamentos);
        Assert.Equal(FormaPagamento.Pix, pagamento.Forma);
    }

    [Fact]
    public async Task NotasConsecutivas_DevemTerNumerosSequenciais()
    {
        await AbrirNovoCaixa();
        var primeira = await ComandaPagaEFechada();
        var segunda = await ComandaPagaEFechada();

        var notaPrimeira = await GetAsync<NotaFiscalResponse>(
            $"api/comandas/{primeira.Id}/nota-fiscal"
        );
        var notaSegunda = await GetAsync<NotaFiscalResponse>(
            $"api/comandas/{segunda.Id}/nota-fiscal"
        );

        Assert.Equal(notaPrimeira.Numero + 1, notaSegunda.Numero);
        Assert.NotEqual(notaPrimeira.ChaveAcesso, notaSegunda.ChaveAcesso);
    }

    [Fact]
    public async Task NotaDeComandaAberta_DeveRetornar404()
    {
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);

        var response = await Client.GetAsync($"api/comandas/{comanda.Id}/nota-fiscal");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ComandaResponse> ComandaPagaEFechada()
    {
        var categoria = await CriarCategoria();
        var pizza = await CriarProduto(categoria.Id, "Pizza", 60m);
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);
        var url = $"api/comandas/{comanda.Id}";

        comanda = await PostAsync<ComandaResponse>(
            $"{url}/itens",
            new AdicionarItemRequest(pizza.Id, 1, null)
        );

        foreach (var item in comanda.Itens)
            for (var i = 0; i < 3; i++)
                await PatchAsync<ComandaResponse>($"{url}/itens/{item.Id}/avancar");

        await PostAsync<ComandaResponse>(
            $"{url}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 66m)
        );

        return await PostAsync<ComandaResponse>($"{url}/fechar");
    }
}

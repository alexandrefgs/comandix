using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class PagamentosEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task RegistrarPagamento_SemCaixaAberto_DeveRetornar400()
    {
        var caixa = await AbrirNovoCaixa();
        await PostAsync<CaixaResponse>($"api/caixas/{caixa.Id}/fechar", new FecharCaixaRequest(100m));
        var comanda = await NovaComandaComPizza();

        var response = await EnviarPost(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 66m)
        );

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal(
            "Não há caixa aberto. Abra o caixa antes de registrar pagamentos.",
            problema.Detail
        );
    }

    [Fact]
    public async Task FecharComanda_ComSaldoPendente_DeveRetornar400()
    {
        await AbrirNovoCaixa();
        var comanda = await NovaComandaComPizza();
        await EntregarItens(comanda);
        await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 30m)
        );

        var response = await EnviarPost($"api/comandas/{comanda.Id}/fechar");

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal("A comanda ainda possui saldo a pagar.", problema.Detail);
    }

    [Fact]
    public async Task PagamentoEmDinheiro_DeveFecharCaixaSemDiferenca()
    {
        var caixa = await AbrirNovoCaixa(100m);
        var comanda = await NovaComandaComPizza();
        await EntregarItens(comanda);

        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Dinheiro, 66m, 100m)
        );
        Assert.Equal(34m, comanda.Pagamentos.Single().Troco);

        await PostAsync<ComandaResponse>($"api/comandas/{comanda.Id}/fechar");

        var fechado = await PostAsync<CaixaResponse>(
            $"api/caixas/{caixa.Id}/fechar",
            new FecharCaixaRequest(166m)
        );
        Assert.Equal(166m, fechado.DinheiroEsperado);
        Assert.Equal(0m, fechado.Diferenca);
    }

    [Fact]
    public async Task Estornar_DeveDevolverSaldoEManterHistorico()
    {
        await AbrirNovoCaixa();
        var comanda = await NovaComandaComPizza();
        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.CartaoDebito, 66m)
        );
        var pagamento = comanda.Pagamentos.Single();

        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos/{pagamento.Id}/estornar",
            new EstornarPagamentoRequest("Lançado na forma errada")
        );

        Assert.Equal(66m, comanda.Saldo);
        var estornado = Assert.Single(comanda.Pagamentos);
        Assert.True(estornado.Estornado);
        Assert.Equal("Lançado na forma errada", estornado.MotivoEstorno);
    }

    [Fact]
    public async Task Estornar_PagamentoDeCaixaFechado_DeveRetornar400()
    {
        var caixa = await AbrirNovoCaixa();
        var comanda = await NovaComandaComPizza();
        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 30m)
        );
        await PostAsync<CaixaResponse>($"api/caixas/{caixa.Id}/fechar", new FecharCaixaRequest(100m));
        await AbrirNovoCaixa();

        var response = await EnviarPost(
            $"api/comandas/{comanda.Id}/pagamentos/{comanda.Pagamentos.Single().Id}/estornar",
            new EstornarPagamentoRequest("Erro")
        );

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal(
            "O pagamento pertence a um caixa já fechado e não pode ser estornado.",
            problema.Detail
        );
    }

    [Fact]
    public async Task Garcom_NaoPodeEstornar_DeveRetornar403()
    {
        await AbrirNovoCaixa();
        var comanda = await NovaComandaComPizza();
        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/pagamentos",
            new RegistrarPagamentoRequest(FormaPagamento.Pix, 66m)
        );
        AutenticarComo(await CriarGarcom());

        var response = await EnviarPost(
            $"api/comandas/{comanda.Id}/pagamentos/{comanda.Pagamentos.Single().Id}/estornar",
            new EstornarPagamentoRequest("Tentativa")
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<ComandaResponse> NovaComandaComPizza()
    {
        var categoria = await CriarCategoria();
        var pizza = await CriarProduto(categoria.Id, "Pizza", 60m);
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);

        return await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/itens",
            new AdicionarItemRequest(pizza.Id, 1, null)
        );
    }

    private async Task EntregarItens(ComandaResponse comanda)
    {
        foreach (var item in comanda.Itens)
            for (var i = 0; i < 3; i++)
                await PatchAsync<ComandaResponse>($"api/comandas/{comanda.Id}/itens/{item.Id}/avancar");
    }
}
using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class CaixaEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Abrir_DeveRetornarCaixaAbertoComVendasZeradas()
    {
        var caixa = await AbrirNovoCaixa(100m);

        Assert.Equal(StatusCaixa.Aberto, caixa.Status);
        Assert.Equal(100m, caixa.ValorAbertura);
        Assert.Equal(4, caixa.Vendas.Count());
        Assert.Equal(0m, caixa.TotalVendas);

        var atual = await GetAsync<CaixaResponse>("api/caixas/atual");
        Assert.Equal(caixa.Id, atual.Id);
    }

    [Fact]
    public async Task Abrir_ComCaixaJaAberto_DeveRetornar409()
    {
        await AbrirNovoCaixa();

        var response = await EnviarPost("api/caixas", new AbrirCaixaRequest(50m));

        var problema = await LerProblema(response, HttpStatusCode.Conflict);
        Assert.Equal("Já existe um caixa aberto.", problema.Detail);
    }

    [Fact]
    public async Task SuprimentoESangria_DevemEntrarNoDinheiroEsperado()
    {
        var caixa = await AbrirNovoCaixa(100m);

        await PostAsync<CaixaResponse>(
            $"api/caixas/{caixa.Id}/suprimento",
            new MovimentacaoCaixaRequest(50m, "Reforço de troco")
        );
        var resumo = await PostAsync<CaixaResponse>(
            $"api/caixas/{caixa.Id}/sangria",
            new MovimentacaoCaixaRequest(30m, "Cofre")
        );

        Assert.Equal(50m, resumo.TotalSuprimentos);
        Assert.Equal(30m, resumo.TotalSangrias);
        Assert.Equal(120m, resumo.DinheiroEsperado);
        Assert.Equal(2, resumo.Movimentacoes.Count());
    }

    [Fact]
    public async Task Fechar_DeveCalcularDiferencaELiberarAberturaDeNovoCaixa()
    {
        var caixa = await AbrirNovoCaixa(100m);

        var fechado = await PostAsync<CaixaResponse>(
            $"api/caixas/{caixa.Id}/fechar",
            new FecharCaixaRequest(95m)
        );

        Assert.Equal(StatusCaixa.Fechado, fechado.Status);
        Assert.Equal(100m, fechado.DinheiroEsperado);
        Assert.Equal(-5m, fechado.Diferenca);

        var atual = await Client.GetAsync("api/caixas/atual");
        Assert.Equal(HttpStatusCode.NotFound, atual.StatusCode);
    }

    [Fact]
    public async Task Garcom_NaoPodeAbrirCaixa_DeveRetornar403()
    {
        AutenticarComo(await CriarGarcom());

        var response = await EnviarPost("api/caixas", new AbrirCaixaRequest(100m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

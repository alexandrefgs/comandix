using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class AutorizacaoTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task RequisicaoSemToken_DeveRetornar401()
    {
        var anonimo = Factory.CreateClient();

        var response = await anonimo.GetAsync("api/categorias");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Garcom_NaoPodeCriarCategoria_DeveRetornar403()
    {
        AutenticarComo(await CriarGarcom());

        var response = await EnviarPost("api/categorias", new CategoriaRequest("Sobremesas"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cozinha_NaoPodeAbrirComanda_DeveRetornar403()
    {
        var mesa = await CriarMesa();
        AutenticarComo(await CriarFuncionario(Cargo.Cozinha));

        var response = await EnviarPost("api/comandas", new AbrirComandaRequest(mesa.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Garcom_AoAbrirComanda_DeveSerOResponsavelMesmoInformandoOutro()
    {
        var mesa = await CriarMesa();
        var outroGarcom = await CriarGarcom();
        var garcom = await CriarGarcom();
        AutenticarComo(garcom);

        var comanda = await PostAsync<ComandaResponse>(
            "api/comandas",
            new AbrirComandaRequest(mesa.Id, outroGarcom.Id)
        );

        Assert.Equal(garcom.Id, comanda.GarcomId);
    }

    [Fact]
    public async Task Gerente_AoAbrirComandaSemGarcom_DeveRetornar400()
    {
        var mesa = await CriarMesa();

        var response = await EnviarPost("api/comandas", new AbrirComandaRequest(mesa.Id));

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal("Informe o garçom responsável pela comanda.", problema.Detail);
    }
}

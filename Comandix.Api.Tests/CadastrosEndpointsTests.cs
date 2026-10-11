using System.Net;
using System.Net.Http.Json;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;

namespace Comandix.Api.Tests;

public class CadastrosEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task CriarMesa_ComNumeroDuplicado_DeveRetornar409()
    {
        var mesa = await CriarMesa();

        var response = await EnviarPost("api/mesas", new MesaRequest(mesa.Numero, 2));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CriarMesa_ComCapacidadeZero_DeveRetornar400()
    {
        var response = await EnviarPost("api/mesas", new MesaRequest(9999, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CriarFuncionario_ComCargoInvalido_DeveRetornar400()
    {
        var response = await Client.PostAsJsonAsync("api/funcionarios", new { nome = "Ana", cargo = "Astronauta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RemoverCategoria_ComProdutos_DeveRetornar409()
    {
        var categoria = await CriarCategoria();
        await CriarProduto(categoria.Id, "Pizza", 60m);

        var response = await Client.DeleteAsync($"api/categorias/{categoria.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoverProduto_JaPedidoEmComanda_DeveRetornar409()
    {
        var categoria = await CriarCategoria();
        var produto = await CriarProduto(categoria.Id, "Pizza", 60m);
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);
        await PostAsync<ComandaResponse>($"api/comandas/{comanda.Id}/itens", new AdicionarItemRequest(produto.Id, 1, null));

        var response = await Client.DeleteAsync($"api/produtos/{produto.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoverProduto_NuncaPedido_DeveRetornar204()
    {
        var categoria = await CriarCategoria();
        var produto = await CriarProduto(categoria.Id, "Pizza", 60m);

        var response = await Client.DeleteAsync($"api/produtos/{produto.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
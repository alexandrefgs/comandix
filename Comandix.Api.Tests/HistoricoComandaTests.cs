using System.Net;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class HistoricoComandaTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Historico_DeveRegistrarQuemFezCadaAcaoEManterTaxaComQuemAbriu()
    {
        var gerenteId = Guid.NewGuid();
        AutenticarComo(gerenteId, "Ana Gerente", Cargo.Gerente);

        var categoria = await CriarCategoria();
        var pizza = await CriarProduto(categoria.Id, "Pizza", 60m);
        var garcomQueAbriu = await CriarGarcom();
        var outroGarcom = await CriarGarcom();
        var comanda = await AbrirComanda((await CriarMesa()).Id, garcomQueAbriu.Id);

        AutenticarComo(outroGarcom);
        comanda = await PostAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/itens",
            new AdicionarItemRequest(pizza.Id, 2, null)
        );
        comanda = await PatchAsync<ComandaResponse>(
            $"api/comandas/{comanda.Id}/itens/{comanda.Itens.Single().Id}/cancelar"
        );

        Assert.Equal(garcomQueAbriu.Id, comanda.GarcomId);

        AutenticarComo(gerenteId, "Ana Gerente", Cargo.Gerente);
        var historico = await GetAsync<List<EventoComandaResponse>>(
            $"api/comandas/{comanda.Id}/historico"
        );

        Assert.Collection(
            historico,
            abertura =>
            {
                Assert.Equal(TipoEventoComanda.Abertura, abertura.Tipo);
                Assert.Equal("Ana Gerente", abertura.Funcionario);
                Assert.Contains(garcomQueAbriu.Nome, abertura.Descricao);
            },
            adicionado =>
            {
                Assert.Equal(TipoEventoComanda.ItemAdicionado, adicionado.Tipo);
                Assert.Equal(outroGarcom.Id, adicionado.FuncionarioId);
                Assert.Equal("Adicionou 2× Pizza", adicionado.Descricao);
                Assert.Equal(120m, adicionado.Valor);
            },
            cancelado =>
            {
                Assert.Equal(TipoEventoComanda.ItemCancelado, cancelado.Tipo);
                Assert.Equal(outroGarcom.Id, cancelado.FuncionarioId);
            }
        );
    }

    [Fact]
    public async Task Historico_Garcom_DeveRetornar403()
    {
        var comanda = await AbrirComanda((await CriarMesa()).Id, (await CriarGarcom()).Id);
        AutenticarComo(await CriarGarcom());

        var response = await Client.GetAsync($"api/comandas/{comanda.Id}/historico");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
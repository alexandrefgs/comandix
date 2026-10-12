using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Comandix.Api.Tests.Infra;

[Collection(ApiCollection.Nome)]
public abstract class ApiTestBase
{
    private static int _proximoNumeroMesa = 100;

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected ComandixApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected ApiTestBase(ComandixApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        AutenticarComo(Guid.NewGuid(), "Gerente Teste", Cargo.Gerente);
    }

    protected void AutenticarComo(FuncionarioResponse funcionario) =>
        AutenticarComo(funcionario.Id, funcionario.Nome, funcionario.Cargo);

    protected void AutenticarComo(Guid id, string nome, Cargo cargo)
    {
        var token = Factory
            .Services.GetRequiredService<TokenService>()
            .Gerar(id, nome, cargo)
            .Token;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected Task<HttpResponseMessage> EnviarPost(string url, object? body = null) =>
        body is null ? Client.PostAsync(url, null) : Client.PostAsJsonAsync(url, body, Json);

    protected async Task<T> GetAsync<T>(string url) => await Ler<T>(await Client.GetAsync(url));

    protected async Task<T> PostAsync<T>(string url, object? body = null) =>
        await Ler<T>(await EnviarPost(url, body));

    protected async Task<T> PutAsync<T>(string url, object body) =>
        await Ler<T>(await Client.PutAsJsonAsync(url, body, Json));

    protected async Task<T> PatchAsync<T>(string url, object? body = null) =>
        await Ler<T>(
            body is null
                ? await Client.PatchAsync(url, null)
                : await Client.PatchAsJsonAsync(url, body, Json)
        );

    protected static async Task<ProblemaResponse> LerProblema(
        HttpResponseMessage response,
        HttpStatusCode esperado
    )
    {
        Assert.Equal(esperado, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProblemaResponse>(Json))!;
    }

    protected Task<CategoriaResponse> CriarCategoria() =>
        PostAsync<CategoriaResponse>(
            "api/categorias",
            new CategoriaRequest($"Categoria {Guid.NewGuid():N}")
        );

    protected Task<ProdutoResponse> CriarProduto(Guid categoriaId, string nome, decimal preco) =>
        PostAsync<ProdutoResponse>(
            "api/produtos",
            new ProdutoRequest(nome, null, preco, categoriaId)
        );

    protected Task<MesaResponse> CriarMesa() =>
        PostAsync<MesaResponse>(
            "api/mesas",
            new MesaRequest(Interlocked.Increment(ref _proximoNumeroMesa), 4)
        );

    protected Task<FuncionarioResponse> CriarGarcom() => CriarFuncionario(Cargo.Garcom);

    protected Task<FuncionarioResponse> CriarFuncionario(Cargo cargo) =>
        PostAsync<FuncionarioResponse>(
            "api/funcionarios",
            new FuncionarioRequest($"{cargo} Teste", cargo)
        );

    protected Task<ComandaResponse> AbrirComanda(Guid mesaId, Guid garcomId) =>
        PostAsync<ComandaResponse>("api/comandas", new AbrirComandaRequest(mesaId, garcomId));

    protected async Task<CaixaResponse> AbrirNovoCaixa(decimal valorAbertura = 100m)
    {
        var atual = await Client.GetAsync("api/caixas/atual");
        if (atual.IsSuccessStatusCode)
        {
            var caixa = (await atual.Content.ReadFromJsonAsync<CaixaResponse>(Json))!;
            await PostAsync<CaixaResponse>(
                $"api/caixas/{caixa.Id}/fechar",
                new FecharCaixaRequest(0m)
            );
        }

        return await PostAsync<CaixaResponse>("api/caixas", new AbrirCaixaRequest(valorAbertura));
    }

    private static async Task<T> Ler<T>(HttpResponseMessage response)
    {
        var conteudo = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {conteudo}");
        return JsonSerializer.Deserialize<T>(conteudo, Json)!;
    }

    protected record ProblemaResponse(int Status, string? Title, string? Detail);
}

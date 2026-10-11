using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Comandix.Api.Dtos;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests.Infra;

[Collection(ApiCollection.Nome)]
public abstract class ApiTestBase(ComandixApiFactory factory)
{
    private static int _proximoNumeroMesa = 100;

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected HttpClient Client { get; } = factory.CreateClient();

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

    protected Task<FuncionarioResponse> CriarGarcom() =>
        PostAsync<FuncionarioResponse>(
            "api/funcionarios",
            new FuncionarioRequest("Garçom Teste", Cargo.Garcom)
        );

    protected Task<ComandaResponse> AbrirComanda(Guid mesaId, Guid garcomId) =>
        PostAsync<ComandaResponse>("api/comandas", new AbrirComandaRequest(mesaId, garcomId));

    private static async Task<T> Ler<T>(HttpResponseMessage response)
    {
        var conteudo = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {conteudo}");
        return JsonSerializer.Deserialize<T>(conteudo, Json)!;
    }

    protected record ProblemaResponse(int Status, string? Title, string? Detail);
}

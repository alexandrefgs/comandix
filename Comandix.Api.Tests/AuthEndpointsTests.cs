using System.Net;
using System.Net.Http.Json;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class AuthEndpointsTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Login_DoGerenteInicial_DeveRetornarToken()
    {
        var login = await PostAsync<LoginResponse>(
            "api/auth/login",
            new LoginRequest("gerente", "Gerente@Teste123")
        );

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.Equal(Cargo.Gerente, login.Cargo);
        Assert.True(login.ExpiraEm > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornar401()
    {
        var response = await EnviarPost(
            "api/auth/login",
            new LoginRequest("gerente", "SenhaErrada123")
        );

        var problema = await LerProblema(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Usuário ou senha inválidos.", problema.Detail);
    }

    [Fact]
    public async Task DefinirCredenciais_ParaCaixa_DevePermitirLogin()
    {
        var caixa = await PostAsync<FuncionarioResponse>(
            "api/funcionarios",
            new FuncionarioRequest("Maria", Cargo.Caixa)
        );
        var usuario = $"caixa{Guid.NewGuid():N}";

        var response = await Client.PutAsJsonAsync(
            $"api/funcionarios/{caixa.Id}/credenciais",
            new CredenciaisRequest(usuario, "Caixa@123456"),
            Json
        );
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var login = await PostAsync<LoginResponse>(
            "api/auth/login",
            new LoginRequest(usuario, "Caixa@123456")
        );
        Assert.Equal(Cargo.Caixa, login.Cargo);
    }

    [Fact]
    public async Task DefinirCredenciais_ParaGarcom_DeveRetornar400()
    {
        var garcom = await CriarGarcom();

        var response = await Client.PutAsJsonAsync(
            $"api/funcionarios/{garcom.Id}/credenciais",
            new CredenciaisRequest("carlos", "Garcom@123456"),
            Json
        );

        var problema = await LerProblema(response, HttpStatusCode.BadRequest);
        Assert.Equal("Apenas gerente e caixa acessam com usuário e senha.", problema.Detail);
    }
}

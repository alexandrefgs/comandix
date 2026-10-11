using System.Net;
using System.Net.Http.Headers;
using Comandix.Api.Dtos;
using Comandix.Api.Tests.Infra;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;

namespace Comandix.Api.Tests;

public class AuthQrCodeTests(ComandixApiFactory factory) : ApiTestBase(factory)
{
    private const string Pin = "4821";

    [Fact]
    public async Task PrimeiroAcesso_DeveIdentificarDefinirPinERetornarToken()
    {
        var (garcom, codigo) = await CriarGarcomComCodigo();

        var identificacao = await PostAsync<IdentificacaoQrCodeResponse>(
            "api/auth/qrcode/identificar",
            new IdentificarQrCodeRequest(codigo)
        );
        Assert.Equal(garcom.Id, identificacao.FuncionarioId);
        Assert.False(identificacao.PossuiPin);

        var login = await PostAsync<LoginResponse>(
            "api/auth/qrcode/definir-pin",
            new QrCodePinRequest(codigo, Pin)
        );
        Assert.Equal(garcom.Id, login.FuncionarioId);
        Assert.Equal(Cargo.Garcom, login.Cargo);
    }

    [Fact]
    public async Task Login_ComPinCorreto_DeveRetornarToken()
    {
        var (garcom, codigo) = await CriarGarcomComPin();

        var login = await PostAsync<LoginResponse>(
            "api/auth/qrcode/login",
            new QrCodePinRequest(codigo, Pin)
        );

        Assert.Equal(garcom.Id, login.FuncionarioId);
    }

    [Fact]
    public async Task Login_ComCodigoInvalido_DeveRetornar401()
    {
        var response = await EnviarPost(
            "api/auth/qrcode/login",
            new QrCodePinRequest("codigo-que-nao-existe", Pin)
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DefinirPin_QuandoJaDefinido_DeveRetornar400()
    {
        var (_, codigo) = await CriarGarcomComPin();

        var response = await EnviarPost(
            "api/auth/qrcode/definir-pin",
            new QrCodePinRequest(codigo, "9999")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_AposCincoPinsErrados_DeveBloquearMesmoComPinCorreto()
    {
        var (_, codigo) = await CriarGarcomComPin();

        for (var i = 1; i < Funcionario.MaximoTentativasPin; i++)
        {
            var erro = await EnviarPost("api/auth/qrcode/login", new QrCodePinRequest(codigo, "0000"));
            Assert.Equal(HttpStatusCode.Unauthorized, erro.StatusCode);
        }

        var bloqueio = await EnviarPost("api/auth/qrcode/login", new QrCodePinRequest(codigo, "0000"));
        Assert.Equal(HttpStatusCode.Forbidden, bloqueio.StatusCode);

        var comPinCorreto = await EnviarPost("api/auth/qrcode/login", new QrCodePinRequest(codigo, Pin));
        Assert.Equal(HttpStatusCode.Forbidden, comPinCorreto.StatusCode);
    }

    [Fact]
    public async Task Desbloquear_DevePermitirLoginNovamente()
    {
        var (garcom, codigo) = await CriarGarcomComPin();
        for (var i = 0; i < Funcionario.MaximoTentativasPin; i++)
            await EnviarPost("api/auth/qrcode/login", new QrCodePinRequest(codigo, "0000"));

        var response = await EnviarPost($"api/funcionarios/{garcom.Id}/desbloquear");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var login = await PostAsync<LoginResponse>(
            "api/auth/qrcode/login",
            new QrCodePinRequest(codigo, Pin)
        );
        Assert.Equal(garcom.Id, login.FuncionarioId);
    }

    [Fact]
    public async Task RegenerarCodigo_DeveInvalidarCodigoAntigoEManterPin()
    {
        var (garcom, codigoAntigo) = await CriarGarcomComPin();

        var novo = await PostAsync<CodigoAcessoResponse>($"api/funcionarios/{garcom.Id}/codigo-acesso");

        var comAntigo = await EnviarPost(
            "api/auth/qrcode/login",
            new QrCodePinRequest(codigoAntigo, Pin)
        );
        Assert.Equal(HttpStatusCode.Unauthorized, comAntigo.StatusCode);

        var login = await PostAsync<LoginResponse>(
            "api/auth/qrcode/login",
            new QrCodePinRequest(novo.Codigo, Pin)
        );
        Assert.Equal(garcom.Id, login.FuncionarioId);
    }

    [Fact]
    public async Task TokenDoQrCode_DevePermitirGarcomAbrirComanda()
    {
        var mesa = await CriarMesa();
        var (garcom, codigo) = await CriarGarcomComPin();
        var login = await PostAsync<LoginResponse>(
            "api/auth/qrcode/login",
            new QrCodePinRequest(codigo, Pin)
        );

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var comanda = await PostAsync<ComandaResponse>("api/comandas", new AbrirComandaRequest(mesa.Id));

        Assert.Equal(garcom.Id, comanda.GarcomId);
    }

    private async Task<(FuncionarioResponse Garcom, string Codigo)> CriarGarcomComCodigo()
    {
        var garcom = await CriarGarcom();
        var acesso = await PostAsync<CodigoAcessoResponse>($"api/funcionarios/{garcom.Id}/codigo-acesso");
        return (garcom, acesso.Codigo);
    }

    private async Task<(FuncionarioResponse Garcom, string Codigo)> CriarGarcomComPin()
    {
        var (garcom, codigo) = await CriarGarcomComCodigo();
        await PostAsync<LoginResponse>("api/auth/qrcode/definir-pin", new QrCodePinRequest(codigo, Pin));
        return (garcom, codigo);
    }
}
using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comandix.Api.Controllers;

[Route("api/auth")]
public class AuthController(
    ComandixDbContext context,
    IPasswordHasher<Funcionario> hasher,
    TokenService tokenService
) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = request.Usuario.Trim().ToLowerInvariant();
        var funcionario = await context.Funcionarios.FirstOrDefaultAsync(f => f.Usuario == usuario);

        if (
            funcionario is null
            || !funcionario.Ativo
            || funcionario.SenhaHash is null
            || hasher.VerifyHashedPassword(funcionario, funcionario.SenhaHash, request.Senha)
                == PasswordVerificationResult.Failed
        )
            return NaoAutorizado("Usuário ou senha inválidos.");

        return Ok(tokenService.Gerar(funcionario));
    }

    [HttpPost("qrcode/identificar")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IdentificacaoQrCodeResponse>> IdentificarQrCode(
        IdentificarQrCodeRequest request
    )
    {
        var funcionario = await BuscarPorCodigo(request.Codigo);
        if (funcionario is null)
            return NaoAutorizado("Código de acesso inválido.");

        return Ok(
            new IdentificacaoQrCodeResponse(
                funcionario.Id,
                funcionario.Nome,
                funcionario.Cargo,
                funcionario.PossuiPin,
                funcionario.AcessoBloqueado
            )
        );
    }

    [HttpPost("qrcode/definir-pin")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> DefinirPin(QrCodePinRequest request)
    {
        var funcionario = await BuscarPorCodigo(request.Codigo);
        if (funcionario is null)
            return NaoAutorizado("Código de acesso inválido.");

        if (funcionario.AcessoBloqueado)
            return Bloqueado();

        funcionario.DefinirPin(hasher.HashPassword(funcionario, request.Pin));
        await context.SaveChangesAsync();

        return Ok(tokenService.Gerar(funcionario));
    }

    [HttpPost("qrcode/login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> LoginQrCode(QrCodePinRequest request)
    {
        var funcionario = await BuscarPorCodigo(request.Codigo);
        if (funcionario is null)
            return NaoAutorizado("Código de acesso inválido.");

        if (funcionario.AcessoBloqueado)
            return Bloqueado();

        if (!funcionario.PossuiPin)
            return Conflito("PIN ainda não definido. Faça o primeiro acesso.");

        if (
            hasher.VerifyHashedPassword(funcionario, funcionario.PinHash!, request.Pin)
            == PasswordVerificationResult.Failed
        )
        {
            funcionario.RegistrarFalhaPin();
            await context.SaveChangesAsync();

            if (funcionario.AcessoBloqueado)
                return Bloqueado();

            var restantes = Funcionario.MaximoTentativasPin - funcionario.TentativasPinFalhas;
            return NaoAutorizado($"PIN incorreto. Restam {restantes} tentativa(s).");
        }

        funcionario.RegistrarSucessoPin();
        await context.SaveChangesAsync();

        return Ok(tokenService.Gerar(funcionario));
    }

    private Task<Funcionario?> BuscarPorCodigo(string codigo)
    {
        var hash = CodigoAcesso.CalcularHash(codigo.Trim());
        return context.Funcionarios.FirstOrDefaultAsync(f => f.CodigoAcessoHash == hash && f.Ativo);
    }

    private ObjectResult NaoAutorizado(string mensagem) =>
        Problem(
            detail: mensagem,
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Não autorizado."
        );

    private ObjectResult Bloqueado() =>
        Problem(
            detail: "Acesso bloqueado por excesso de tentativas. Procure o gerente.",
            statusCode: StatusCodes.Status403Forbidden,
            title: "Acesso bloqueado."
        );
}
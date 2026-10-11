using Comandix.Api.Auth;
using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Infrastructure.Data;
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
            return Problem(
                detail: "Usuário ou senha inválidos.",
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Não autorizado."
            );

        return Ok(tokenService.Gerar(funcionario));
    }
}
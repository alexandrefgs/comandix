using Comandix.Api.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;


namespace Comandix.Api.Controllers;

[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid FuncionarioLogadoId =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    protected bool UsuarioEhGarcom => User.IsInRole(Perfis.Garcom);

    protected ObjectResult RequisicaoInvalida(string mensagem) =>
        Problem(
            detail: mensagem,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Requisição inválida."
        );

    protected ObjectResult Conflito(string mensagem) =>
        Problem(
            detail: mensagem,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflito com o estado atual."
        );
}
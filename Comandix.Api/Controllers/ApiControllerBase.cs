using Microsoft.AspNetCore.Mvc;

namespace Comandix.Api.Controllers;

[ApiController]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
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

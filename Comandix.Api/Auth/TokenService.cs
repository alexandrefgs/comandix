using Comandix.Api.Dtos;
using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Comandix.Api.Auth;

public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public LoginResponse Gerar(Funcionario funcionario) =>
        Gerar(funcionario.Id, funcionario.Nome, funcionario.Cargo);

    public LoginResponse Gerar(Guid funcionarioId, string nome, Cargo cargo)
    {
        var expiraEm = DateTime.UtcNow.AddHours(_jwt.ExpiracaoHoras);

        var token = _handler.CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = _jwt.Issuer,
                Audience = _jwt.Audience,
                Expires = expiraEm,
                Claims = new Dictionary<string, object>
                {
                    [JwtRegisteredClaimNames.Sub] = funcionarioId.ToString(),
                    [JwtRegisteredClaimNames.Name] = nome,
                    ["role"] = cargo.ToString(),
                },
                SigningCredentials = new SigningCredentials(
                    _jwt.ObterChaveAssinatura(),
                    SecurityAlgorithms.HmacSha256
                ),
            }
        );

        return new LoginResponse(token, expiraEm, funcionarioId, nome, cargo);
    }
}
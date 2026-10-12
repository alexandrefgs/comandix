using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Comandix.Api.Auth;

public class JwtOptions
{
    public const string Secao = "Jwt";

    public string Issuer { get; set; } = "Comandix";
    public string Audience { get; set; } = "Comandix";
    public string Chave { get; set; } = string.Empty;
    public int ExpiracaoHoras { get; set; } = 12;

    public SymmetricSecurityKey ObterChaveAssinatura() => new(Encoding.UTF8.GetBytes(Chave));
}

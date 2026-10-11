using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Comandix.Api.Auth;

public static class CodigoAcesso
{
    public static (string Codigo, string Hash) Gerar()
    {
        var codigo = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (codigo, CalcularHash(codigo));
    }

    public static string CalcularHash(string codigo) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo)));
}
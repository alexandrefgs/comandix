namespace Comandix.Domain.Fiscal;

public static class ChaveAcessoNfce
{
    public const int Modelo = 65;
    public const int TipoEmissaoNormal = 1;

    public static string Gerar(
        int codigoUf,
        DateTime emissao,
        string cnpj,
        int serie,
        int numero,
        int codigoNumerico
    )
    {
        if (cnpj.Length != 14 || !cnpj.All(char.IsDigit))
            throw new ArgumentException("O CNPJ deve ter 14 dígitos.", nameof(cnpj));

        var semDigito =
            $"{codigoUf:D2}{emissao:yyMM}{cnpj}{Modelo:D2}{serie:D3}{numero:D9}{TipoEmissaoNormal}{codigoNumerico:D8}";

        if (semDigito.Length != 43)
            throw new ArgumentException("Os dados informados não formam uma chave válida.");

        return semDigito + CalcularDigitoVerificador(semDigito);
    }

    public static int CalcularDigitoVerificador(string chaveSemDigito)
    {
        var soma = 0;
        var peso = 2;

        for (var i = chaveSemDigito.Length - 1; i >= 0; i--)
        {
            soma += (chaveSemDigito[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}

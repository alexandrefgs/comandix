namespace Comandix.Api.Auth;

public static class Perfis
{
    public const string Gerente = "Gerente";
    public const string Garcom = "Garcom";
    public const string Cozinha = "Cozinha";
    public const string Caixa = "Caixa";

    public const string Salao = $"{Gerente},{Garcom}";
    public const string Preparo = $"{Gerente},{Garcom},{Cozinha}";
    public const string Fechamento = $"{Gerente},{Garcom},{Caixa}";
}
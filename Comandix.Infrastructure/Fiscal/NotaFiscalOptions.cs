namespace Comandix.Infrastructure.Fiscal;

public class NotaFiscalOptions
{
    public const string Secao = "NotaFiscal";
    public const string ModoSimulacao = "Simulacao";

    public string Modo { get; set; } = string.Empty;
    public int Serie { get; set; } = 1;
    public int CodigoUf { get; set; } = 42;
    public string CnpjEmitente { get; set; } = "00000000000000";
    public string RazaoSocialEmitente { get; set; } = "Comandix Restaurante (Simulação)";
}

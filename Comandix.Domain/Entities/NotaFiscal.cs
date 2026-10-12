namespace Comandix.Domain.Entities;

public class NotaFiscal : EntityBase
{
    public Guid ComandaId { get; private set; }
    public int Serie { get; private set; }
    public int Numero { get; private set; }
    public string ChaveAcesso { get; private set; } = string.Empty;
    public string Protocolo { get; private set; } = string.Empty;
    public decimal ValorTotal { get; private set; }
    public bool Simulada { get; private set; }

    protected NotaFiscal() { }

    public NotaFiscal(
        Guid comandaId,
        int serie,
        int numero,
        string chaveAcesso,
        string protocolo,
        decimal valorTotal,
        bool simulada
    )
    {
        ComandaId = comandaId;
        Serie = serie;
        Numero = numero;
        ChaveAcesso = chaveAcesso;
        Protocolo = protocolo;
        ValorTotal = valorTotal;
        Simulada = simulada;
    }
}

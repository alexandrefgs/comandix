using Comandix.Domain.Enums;

namespace Comandix.Domain.Entities;

public class EventoComanda : EntityBase
{
    public const int TamanhoMaximoDescricao = 500;

    public Guid ComandaId { get; private set; }
    public TipoEventoComanda Tipo { get; private set; }
    public string Descricao { get; private set; } = string.Empty;
    public Guid FuncionarioId { get; private set; }
    public string FuncionarioNome { get; private set; } = string.Empty;
    public decimal? Valor { get; private set; }

    protected EventoComanda() { }

    public EventoComanda(
        Guid comandaId,
        TipoEventoComanda tipo,
        string descricao,
        Guid funcionarioId,
        string funcionarioNome,
        decimal? valor = null
    )
    {
        ComandaId = comandaId;
        Tipo = tipo;
        Descricao =
            descricao.Length <= TamanhoMaximoDescricao
                ? descricao
                : descricao[..(TamanhoMaximoDescricao - 1)] + "…";
        FuncionarioId = funcionarioId;
        FuncionarioNome = funcionarioNome;
        Valor = valor;
    }
}
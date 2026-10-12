using Comandix.Domain.Entities;

namespace Comandix.Domain.Fiscal;

public interface IEmissorNotaFiscal
{
    Task<NotaFiscal> EmitirAsync(Comanda comanda, CancellationToken cancellationToken = default);
}

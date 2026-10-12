import { HttpErrorResponse } from '@angular/common/http';

export function mensagemDeErro(erro: unknown): string {
  if (erro instanceof HttpErrorResponse) {
    if (erro.status === 0) return 'Não foi possível conectar ao servidor.';

    const detalhe: string | undefined = erro.error?.detail;
    if (detalhe) return detalhe;

    const errosValidacao = erro.error?.errors as Record<string, string[]> | undefined;
    const primeiro = errosValidacao ? Object.values(errosValidacao)[0]?.[0] : undefined;
    if (primeiro) return primeiro;
  }

  return 'Ocorreu um erro inesperado. Tente novamente.';
}

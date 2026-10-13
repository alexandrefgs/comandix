import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type StatusItem = 'Pendente' | 'EmPreparo' | 'Pronto' | 'Entregue' | 'Cancelado';
export type StatusComanda = 'Aberta' | 'Fechada' | 'Cancelada';
export type FormaPagamento = 'Dinheiro' | 'Pix' | 'CartaoDebito' | 'CartaoCredito';

export interface ItemComanda {
  id: string;
  produtoId: string;
  nomeProduto: string;
  precoUnitario: number;
  quantidade: number;
  subtotal: number;
  observacao: string | null;
  status: StatusItem;
  pedidoEm: string;
  pagantesIds: string[];
}

export interface DivisaoPessoa {
  pessoaId: string;
  nome: string;
  consumo: number;
  taxaServico: number;
  total: number;
  pago: number;
  saldo: number;
}

export interface PagamentoComanda {
  id: string;
  forma: FormaPagamento;
  valor: number;
  valorRecebido: number;
  troco: number;
  pessoaId: string | null;
  caixaId: string;
  registradoEm: string;
  estornado: boolean;
  motivoEstorno: string | null;
}

export interface ComandaDetalhe {
  id: string;
  mesaId: string;
  mesaNumero: number;
  garcomId: string;
  garcom: string;
  status: StatusComanda;
  abertaEm: string;
  fechadaEm: string | null;
  cobrarTaxaServico: boolean;
  percentualTaxaServico: number;
  consumo: number;
  taxaServico: number;
  total: number;
  totalPago: number;
  saldo: number;
  itens: ItemComanda[];
  divisao: DivisaoPessoa[];
  pagamentos: PagamentoComanda[];
}

export interface NovoPagamento {
  forma: FormaPagamento;
  valor: number;
  valorRecebido?: number;
  pessoaId?: string;
}

export type TipoEventoComanda =
  | 'Abertura'
  | 'ItemAdicionado'
  | 'ItemEmPreparo'
  | 'ItemPronto'
  | 'ItemEntregue'
  | 'ItemCancelado'
  | 'ItemAtribuido'
  | 'PessoasDefinidas'
  | 'PessoaRenomeada'
  | 'TaxaServicoAlterada'
  | 'MesaTrocada'
  | 'PagamentoRegistrado'
  | 'PagamentoEstornado'
  | 'Fechamento'
  | 'Cancelamento';

export interface EventoComanda {
  id: string;
  tipo: TipoEventoComanda;
  descricao: string;
  funcionarioId: string;
  funcionario: string;
  valor: number | null;
  ocorridoEm: string;
}

@Injectable({ providedIn: 'root' })
export class ComandaService {
  private readonly http = inject(HttpClient);

  obter(id: string): Observable<ComandaDetalhe> {
    return this.http.get<ComandaDetalhe>(`/api/comandas/${id}`);
  }

  historico(id: string): Observable<EventoComanda[]> {
    return this.http.get<EventoComanda[]>(`/api/comandas/${id}/historico`);
  }

  adicionarItem(
    id: string,
    produtoId: string,
    quantidade: number,
    observacao: string | null
  ): Observable<ComandaDetalhe> {
    return this.http.post<ComandaDetalhe>(`/api/comandas/${id}/itens`, {
      produtoId,
      quantidade,
      observacao,
    });
  }

  cancelarItem(id: string, itemId: string): Observable<ComandaDetalhe> {
    return this.http.patch<ComandaDetalhe>(`/api/comandas/${id}/itens/${itemId}/cancelar`, null);
  }

  avancarItem(id: string, itemId: string): Observable<ComandaDetalhe> {
    return this.http.patch<ComandaDetalhe>(`/api/comandas/${id}/itens/${itemId}/avancar`, null);
  }

  trocarMesa(id: string, mesaId: string): Observable<ComandaDetalhe> {
    return this.http.post<ComandaDetalhe>(`/api/comandas/${id}/trocar-mesa`, { mesaId });
  }

  definirPessoas(id: string, quantidade: number): Observable<ComandaDetalhe> {
    return this.http.put<ComandaDetalhe>(`/api/comandas/${id}/pessoas`, { quantidade });
  }

  renomearPessoa(id: string, pessoaId: string, nome: string): Observable<ComandaDetalhe> {
    return this.http.patch<ComandaDetalhe>(`/api/comandas/${id}/pessoas/${pessoaId}`, { nome });
  }

  atribuirItem(id: string, itemId: string, pessoaIds: string[]): Observable<ComandaDetalhe> {
    return this.http.put<ComandaDetalhe>(`/api/comandas/${id}/itens/${itemId}/pagantes`, {
      pessoaIds,
    });
  }

  definirTaxaServico(id: string, cobrar: boolean): Observable<ComandaDetalhe> {
    return this.http.patch<ComandaDetalhe>(`/api/comandas/${id}/taxa-servico`, { cobrar });
  }

  registrarPagamento(id: string, pagamento: NovoPagamento): Observable<ComandaDetalhe> {
    return this.http.post<ComandaDetalhe>(`/api/comandas/${id}/pagamentos`, pagamento);
  }

  estornarPagamento(id: string, pagamentoId: string, motivo: string): Observable<ComandaDetalhe> {
    return this.http.post<ComandaDetalhe>(
      `/api/comandas/${id}/pagamentos/${pagamentoId}/estornar`,
      { motivo }
    );
  }

  fechar(id: string): Observable<ComandaDetalhe> {
    return this.http.post<ComandaDetalhe>(`/api/comandas/${id}/fechar`, null);
  }
}
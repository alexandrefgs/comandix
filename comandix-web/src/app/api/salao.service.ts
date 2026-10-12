import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Cargo } from '../auth/auth.service';

export type StatusMesa = 'Livre' | 'Ocupada';

export interface Mesa {
  id: string;
  numero: number;
  capacidade: number;
  ativa: boolean;
  status: StatusMesa;
}

export interface ComandaResumo {
  id: string;
  mesaId: string;
  mesaNumero: number;
  garcomId: string;
  garcom: string;
  status: string;
  abertaEm: string;
  fechadaEm: string | null;
  consumo: number;
  taxaServico: number;
  total: number;
  saldo: number;
  itensProntos: number;
}

export interface Funcionario {
  id: string;
  nome: string;
  cargo: Cargo;
  ativo: boolean;
}

export interface MesaDados {
  numero: number;
  capacidade: number;
}

@Injectable({ providedIn: 'root' })
export class SalaoService {
  private readonly http = inject(HttpClient);

  listarMesas(): Observable<Mesa[]> {
    return this.http.get<Mesa[]>('/api/mesas', { params: { apenasAtivas: true } });
  }

  listarComandasAbertas(): Observable<ComandaResumo[]> {
    return this.http.get<ComandaResumo[]>('/api/comandas');
  }

  listarGarcons(): Observable<Funcionario[]> {
    return this.http.get<Funcionario[]>('/api/funcionarios', {
      params: { cargo: 'Garcom', apenasAtivos: true },
    });
  }

  abrirComanda(mesaId: string, garcomId?: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      '/api/comandas',
      garcomId ? { mesaId, garcomId } : { mesaId },
    );
  }

  criarMesa(dados: MesaDados): Observable<Mesa> {
    return this.http.post<Mesa>('/api/mesas', dados);
  }

  atualizarMesa(id: string, dados: MesaDados): Observable<void> {
    return this.http.put<void>(`/api/mesas/${id}`, dados);
  }

  removerMesa(id: string): Observable<void> {
    return this.http.delete<void>(`/api/mesas/${id}`);
  }
}

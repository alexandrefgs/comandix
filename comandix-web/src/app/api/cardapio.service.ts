import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Categoria {
  id: string;
  nome: string;
  ativa: boolean;
}

export interface Produto {
  id: string;
  nome: string;
  descricao: string | null;
  preco: number;
  disponivel: boolean;
  categoriaId: string;
  categoria: string;
}

export interface ProdutoDados {
  nome: string;
  descricao: string | null;
  preco: number;
  categoriaId: string;
}

@Injectable({ providedIn: 'root' })
export class CardapioService {
  private readonly http = inject(HttpClient);

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>('/api/categorias');
  }

  criarCategoria(nome: string): Observable<Categoria> {
    return this.http.post<Categoria>('/api/categorias', { nome });
  }

  atualizarCategoria(id: string, nome: string): Observable<void> {
    return this.http.put<void>(`/api/categorias/${id}`, { nome });
  }

  removerCategoria(id: string): Observable<void> {
    return this.http.delete<void>(`/api/categorias/${id}`);
  }

  listarProdutos(): Observable<Produto[]> {
    return this.http.get<Produto[]>('/api/produtos');
  }

  criarProduto(dados: ProdutoDados): Observable<Produto> {
    return this.http.post<Produto>('/api/produtos', dados);
  }

  atualizarProduto(id: string, dados: ProdutoDados): Observable<void> {
    return this.http.put<void>(`/api/produtos/${id}`, dados);
  }

  alterarDisponibilidade(id: string, disponivel: boolean): Observable<void> {
    return this.http.patch<void>(
      `/api/produtos/${id}/${disponivel ? 'disponivel' : 'indisponivel'}`,
      null
    );
  }

  removerProduto(id: string): Observable<void> {
    return this.http.delete<void>(`/api/produtos/${id}`);
  }
}
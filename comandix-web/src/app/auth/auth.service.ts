import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

export type Cargo = 'Gerente' | 'Garcom' | 'Cozinha' | 'Caixa';

export interface Sessao {
  token: string;
  expiraEm: string;
  funcionarioId: string;
  nome: string;
  cargo: Cargo;
}

const CHAVE = 'comandix-sessao';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly noNavegador = isPlatformBrowser(inject(PLATFORM_ID));

  private readonly sessaoAtual = signal<Sessao | null>(this.carregar());

  readonly sessao = this.sessaoAtual.asReadonly();
  readonly cargo = computed(() => this.sessaoAtual()?.cargo ?? null);
  readonly autenticado = computed(() => {
    const sessao = this.sessaoAtual();
    return !!sessao && new Date(sessao.expiraEm) > new Date();
  });

  login(usuario: string, senha: string): Observable<Sessao> {
    return this.http
      .post<Sessao>('/api/auth/login', { usuario, senha })
      .pipe(tap((sessao) => this.salvar(sessao)));
  }

  sair(): void {
    this.limpar();
    this.router.navigateByUrl('/login');
  }

  encerrarSessaoExpirada(): void {
    this.limpar();
    this.router.navigate(['/login'], { queryParams: { expirada: 1 } });
  }

  private salvar(sessao: Sessao): void {
    this.sessaoAtual.set(sessao);
    if (this.noNavegador) localStorage.setItem(CHAVE, JSON.stringify(sessao));
  }

  private limpar(): void {
    this.sessaoAtual.set(null);
    if (this.noNavegador) localStorage.removeItem(CHAVE);
  }

  private carregar(): Sessao | null {
    if (!this.noNavegador) return null;

    try {
      const salva = localStorage.getItem(CHAVE);
      return salva ? (JSON.parse(salva) as Sessao) : null;
    } catch {
      return null;
    }
  }
}

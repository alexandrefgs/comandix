import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';

import { AuthService } from '../../auth/auth.service';
import { definirIndexacao } from '../../seo/indexacao';
import { SeletorTema } from '../../tema/seletor-tema';

@Component({
  selector: 'app-painel',
  imports: [ButtonModule, SeletorTema],
  template: `
    <header
      class="flex items-center justify-between border-b border-slate-200 px-4 py-3 sm:px-6 lg:px-10 dark:border-slate-800"
    >
      <span class="text-xl font-bold tracking-tight">
        Comand<span class="text-cyan-700 dark:text-cyan-400">ix</span>
      </span>
      <div class="flex items-center gap-3">
        <app-seletor-tema />
        <p-button label="Sair" severity="secondary" [text]="true" (onClick)="auth.sair()" />
      </div>
    </header>

    <main class="flex flex-col gap-2 px-4 py-10 sm:px-6 lg:px-10">
      <h1 class="text-2xl font-bold tracking-tight">Olá, {{ auth.sessao()?.nome }}!</h1>
      <p class="text-slate-600 dark:text-slate-400">
        Cargo: <strong>{{ auth.cargo() }}</strong>
      </p>
      <p class="text-slate-600 dark:text-slate-400">
        Mesas cadastradas: <strong>{{ mesas()?.length ?? '...' }}</strong>
      </p>
    </main>
  `,
})
export class Painel {
  protected readonly auth = inject(AuthService);
  protected readonly mesas = toSignal(inject(HttpClient).get<unknown[]>('/api/mesas'));

  constructor() {
    definirIndexacao(false);
  }
}
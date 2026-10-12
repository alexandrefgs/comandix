import { Component } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { RouterLink } from '@angular/router';
import { definirIndexacao } from '../../seo/indexacao';

import { SeletorTema } from '../../tema/seletor-tema';

@Component({
  selector: 'app-inicio',
  imports: [ButtonModule, TagModule, RouterLink, SeletorTema],
  template: `
    <header
      class="flex items-center justify-between border-b border-slate-200 px-4 py-3 sm:px-6 lg:px-10 dark:border-slate-800"
    >
      <span class="text-xl font-bold tracking-tight">
        Comand<span class="text-cyan-700 dark:text-cyan-400">ix</span>
      </span>
      <div class="flex items-center gap-3">
        <app-seletor-tema />
        <a
          routerLink="/login"
          class="rounded-lg px-3 py-2 text-sm font-medium text-cyan-700 hover:bg-cyan-50 dark:text-cyan-400 dark:hover:bg-cyan-950"
        >
          Entrar
        </a>
      </div>
    </header>

    <main class="flex flex-col gap-8 px-4 py-10 sm:px-6 lg:px-10">
      <div class="flex flex-col gap-3">
        <h1 class="text-4xl font-bold tracking-tight">Comandas, cozinha e caixa num só lugar.</h1>
        <p class="text-lg text-slate-600 dark:text-slate-400">
          Página provisória para validar o tema, as cores e os componentes.
        </p>
      </div>

      <div class="flex flex-wrap gap-3">
        <p-button label="Botão principal" />
        <p-button label="Secundário" severity="secondary" />
        <p-button label="Contorno" [outlined]="true" />
      </div>

      <div class="flex flex-wrap gap-2">
        <p-tag value="Pago" severity="success" />
        <p-tag value="Em preparo" severity="warn" />
        <p-tag value="Cancelado" severity="danger" />
        <p-tag value="Ocupada" />
      </div>
    </main>
  `,
})
export class Inicio {
  constructor() {
    definirIndexacao(true);
  }
}

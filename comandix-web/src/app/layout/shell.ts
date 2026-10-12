import { isPlatformBrowser } from '@angular/common';
import { Component, PLATFORM_ID, computed, effect, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LogOut, LucideAngularModule, Menu, PanelLeftClose, PanelLeftOpen, X } from 'lucide-angular';

import { AuthService } from '../auth/auth.service';
import { ROTULO_CARGO } from '../auth/rotas-por-cargo';
import { definirIndexacao } from '../seo/indexacao';
import { SeletorTema } from '../tema/seletor-tema';
import { ITENS_MENU } from './menu';

const CHAVE_RECOLHIDO = 'comandix-menu-recolhido';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideAngularModule, SeletorTema],
  template: `
    <div class="flex h-dvh overflow-hidden">
      @if (menuMovelAberto()) {
        <div
          class="fixed inset-0 z-30 bg-slate-950/60 lg:hidden"
          aria-hidden="true"
          (click)="menuMovelAberto.set(false)"
        ></div>
      }

      <aside
        class="fixed inset-y-0 left-0 z-40 flex flex-col overflow-hidden border-r border-slate-200 bg-white transition-all duration-200 lg:static dark:border-slate-800 dark:bg-slate-900"
        [class]="classesMenu()"
      >
        <div class="flex h-14 shrink-0 items-center gap-2 border-b border-slate-200 px-4 dark:border-slate-800">
          <img src="favicon.svg" alt="" width="28" height="28" class="shrink-0" />
          <span class="text-lg font-bold tracking-tight" [class]="esconderNoRecolhido()">
            Comand<span class="text-cyan-700 dark:text-cyan-400">ix</span>
          </span>
          <button
            type="button"
            class="ml-auto rounded-lg p-1.5 text-slate-500 hover:bg-slate-100 lg:hidden dark:hover:bg-slate-800"
            aria-label="Fechar menu"
            (click)="menuMovelAberto.set(false)"
          >
            <lucide-icon [img]="icones.fechar" [size]="18" />
          </button>
        </div>

        <nav class="flex flex-1 flex-col gap-1 overflow-y-auto p-2" aria-label="Menu principal">
          @for (item of itens(); track item.caminho) {
            <a
              [routerLink]="'/app/' + item.caminho"
              routerLinkActive
              #link="routerLinkActive"
              [title]="recolhido() ? item.rotulo : ''"
              (click)="menuMovelAberto.set(false)"
              class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors"
              [class]="link.isActive ? itemAtivo : itemInativo"
            >
              <lucide-icon [img]="item.icone" [size]="18" class="shrink-0" />
              <span class="whitespace-nowrap" [class]="esconderNoRecolhido()">{{ item.rotulo }}</span>
            </a>
          }
        </nav>

        <div class="hidden border-t border-slate-200 p-2 lg:block dark:border-slate-800">
          <button
            type="button"
            class="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
            [attr.aria-label]="recolhido() ? 'Expandir menu' : 'Recolher menu'"
            (click)="recolhido.set(!recolhido())"
          >
            <lucide-icon
              [img]="recolhido() ? icones.expandir : icones.recolher"
              [size]="18"
              class="shrink-0"
            />
            @if (!recolhido()) {
              <span>Recolher</span>
            }
          </button>
        </div>
      </aside>

      <div class="flex min-w-0 flex-1 flex-col">
        <header
          class="flex h-14 shrink-0 items-center gap-3 border-b border-slate-200 bg-white px-4 sm:px-6 lg:px-10 dark:border-slate-800 dark:bg-slate-900"
        >
          <button
            type="button"
            class="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100 lg:hidden dark:hover:bg-slate-800"
            aria-label="Abrir menu"
            (click)="menuMovelAberto.set(true)"
          >
            <lucide-icon [img]="icones.menu" [size]="20" />
          </button>
          <span class="text-lg font-bold tracking-tight lg:hidden">
            Comand<span class="text-cyan-700 dark:text-cyan-400">ix</span>
          </span>

          <div class="ml-auto flex items-center gap-3">
            <app-seletor-tema />
            <div class="hidden text-right leading-tight sm:block">
              <p class="text-sm font-medium">{{ auth.sessao()?.nome }}</p>
              <p class="text-xs text-slate-500 dark:text-slate-400">{{ cargo() }}</p>
            </div>
            <button
              type="button"
              class="rounded-lg p-2 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950 dark:hover:text-red-400"
              aria-label="Sair"
              title="Sair"
              (click)="auth.sair()"
            >
              <lucide-icon [img]="icones.sair" [size]="18" />
            </button>
          </div>
        </header>

        <main class="flex-1 overflow-y-auto px-4 py-6 sm:px-6 lg:px-10">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class Shell {
  protected readonly auth = inject(AuthService);
  private readonly noNavegador = isPlatformBrowser(inject(PLATFORM_ID));

  protected readonly icones = {
    menu: Menu,
    fechar: X,
    sair: LogOut,
    recolher: PanelLeftClose,
    expandir: PanelLeftOpen,
  };

  protected readonly itemAtivo = 'bg-cyan-50 text-cyan-800 dark:bg-cyan-950 dark:text-cyan-300';
  protected readonly itemInativo =
    'text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800';

  protected readonly menuMovelAberto = signal(false);
  protected readonly recolhido = signal(
    this.noNavegador && localStorage.getItem(CHAVE_RECOLHIDO) === 'true'
  );

  protected readonly itens = computed(() => {
    const cargo = this.auth.cargo();
    return ITENS_MENU.filter((item) => cargo && item.cargos.includes(cargo));
  });

  protected readonly cargo = computed(() => {
    const cargo = this.auth.cargo();
    return cargo ? ROTULO_CARGO[cargo] : '';
  });

  protected readonly esconderNoRecolhido = computed(() => (this.recolhido() ? 'lg:hidden' : ''));

  protected readonly classesMenu = computed(() =>
    [
      this.menuMovelAberto() ? 'translate-x-0' : '-translate-x-full',
      'lg:translate-x-0',
      this.recolhido() ? 'w-64 lg:w-16' : 'w-64',
    ].join(' ')
  );

  constructor() {
    definirIndexacao(false);

    effect(() => {
      if (this.noNavegador) localStorage.setItem(CHAVE_RECOLHIDO, String(this.recolhido()));
    });
  }
}
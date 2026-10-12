import { Component, inject } from '@angular/core';
import { LucideAngularModule, Monitor, Moon, Sun } from 'lucide-angular';

import { Tema, TemaService } from './tema.service';

@Component({
  selector: 'app-seletor-tema',
  imports: [LucideAngularModule],
  template: `
    <div
      role="radiogroup"
      aria-label="Tema"
      class="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-white p-1 dark:border-slate-800 dark:bg-slate-900"
    >
      @for (opcao of opcoes; track opcao.valor) {
        <button
          type="button"
          role="radio"
          [attr.aria-checked]="temaService.tema() === opcao.valor"
          [attr.aria-label]="opcao.rotulo"
          [title]="opcao.rotulo"
          (click)="temaService.definir(opcao.valor)"
          class="flex size-8 items-center justify-center rounded-full transition-colors"
          [class]="temaService.tema() === opcao.valor ? ativo : inativo"
        >
          <lucide-icon [img]="opcao.icone" [size]="16" />
        </button>
      }
    </div>
  `,
})
export class SeletorTema {
  protected readonly temaService = inject(TemaService);

  protected readonly ativo = 'bg-cyan-700 text-white dark:bg-cyan-400 dark:text-slate-950';
  protected readonly inativo =
    'text-slate-500 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100';

  protected readonly opcoes: { valor: Tema; rotulo: string; icone: typeof Sun }[] = [
    { valor: 'claro', rotulo: 'Tema claro', icone: Sun },
    { valor: 'sistema', rotulo: 'Tema do sistema', icone: Monitor },
    { valor: 'escuro', rotulo: 'Tema escuro', icone: Moon },
  ];
}

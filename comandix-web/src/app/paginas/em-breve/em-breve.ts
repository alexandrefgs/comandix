import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Construction, LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-em-breve',
  imports: [LucideAngularModule],
  template: `
    <div class="flex flex-col items-center justify-center gap-3 py-24 text-center">
      <div
        class="flex size-14 items-center justify-center rounded-2xl bg-cyan-50 text-cyan-700 dark:bg-cyan-950 dark:text-cyan-400"
      >
        <lucide-icon [img]="icone" [size]="28" />
      </div>
      <h1 class="text-2xl font-bold tracking-tight">{{ titulo }}</h1>
      <p class="max-w-sm text-slate-600 dark:text-slate-400">
        Esta área está em construção e chega nas próximas etapas.
      </p>
    </div>
  `,
})
export class EmBreve {
  protected readonly titulo = inject(ActivatedRoute).snapshot.data['titulo'] as string;
  protected readonly icone = Construction;
}
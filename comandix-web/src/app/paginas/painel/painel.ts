import { HttpClient } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-painel',
  template: `
    <div class="flex flex-col gap-2">
      <h1 class="text-2xl font-bold tracking-tight">Olá, {{ auth.sessao()?.nome }}!</h1>
      <p class="text-slate-600 dark:text-slate-400">
        Mesas cadastradas: <strong>{{ mesas()?.length ?? '...' }}</strong>
      </p>
    </div>
  `,
})
export class Painel {
  protected readonly auth = inject(AuthService);
  protected readonly mesas = toSignal(inject(HttpClient).get<unknown[]>('/api/mesas'));
}

import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject, input, model, signal, untracked } from '@angular/core';
import { MessageService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';

import { ComandaService, EventoComanda, TipoEventoComanda } from '../../api/comanda.service';
import { mensagemDeErro } from '../../api/erros';

const TIPOS_ANULADOS: TipoEventoComanda[] = ['ItemCancelado', 'PagamentoEstornado'];

@Component({
  selector: 'app-historico-comanda',
  imports: [CurrencyPipe, DatePipe, DialogModule],
  template: `
    <p-dialog
      [visible]="visivel()"
      (visibleChange)="visivel.set($event)"
      header="Histórico da mesa"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '40rem' }"
      [breakpoints]="{ '640px': '100vw' }"
      [pt]="{ header: 'relative justify-center', headerActions: 'absolute right-4' }"
    >
      @if (carregando()) {
        <p class="py-10 text-center text-slate-500 dark:text-slate-400">Carregando histórico...</p>
      } @else if (eventos().length === 0) {
        <p class="py-10 text-center text-slate-500 dark:text-slate-400">Nenhum evento registrado.</p>
      } @else {
        <ol class="flex max-h-[60vh] flex-col overflow-y-auto pr-1">
          @for (evento of eventos(); track evento.id) {
            <li class="flex gap-3">
              <div class="flex flex-col items-center">
                <span class="mt-1.5 size-2.5 shrink-0 rounded-full" [class]="corDoTipo[evento.tipo]"></span>
                @if (!$last) {
                  <span class="mt-1 w-px flex-1 bg-slate-200 dark:bg-slate-800"></span>
                }
              </div>

              <div class="min-w-0 flex-1 pb-4">
                <div class="flex flex-wrap items-baseline justify-between gap-x-3">
                  <p class="font-medium">{{ evento.descricao }}</p>
                  @if (evento.valor !== null) {
                    <span
                      class="text-sm tabular-nums"
                      [class.line-through]="anulado(evento)"
                      [class.text-slate-400]="anulado(evento)"
                    >
                      {{ evento.valor | currency }}
                    </span>
                  }
                </div>
                <p class="text-sm text-slate-500 dark:text-slate-400">
                  {{ evento.funcionario }} · {{ evento.ocorridoEm | date: 'dd/MM HH:mm' }}
                </p>
              </div>
            </li>
          }
        </ol>
      }
    </p-dialog>
  `,
})
export class HistoricoComanda {
  readonly comandaId = input.required<string>();
  readonly visivel = model(false);

  private readonly comandas = inject(ComandaService);
  private readonly mensagens = inject(MessageService);

  protected readonly eventos = signal<EventoComanda[]>([]);
  protected readonly carregando = signal(false);

  protected readonly corDoTipo: Record<TipoEventoComanda, string> = {
    Abertura: 'bg-cyan-500',
    Fechamento: 'bg-cyan-500',
    ItemAdicionado: 'bg-emerald-500',
    ItemEntregue: 'bg-emerald-500',
    PagamentoRegistrado: 'bg-emerald-500',
    ItemEmPreparo: 'bg-amber-400',
    ItemPronto: 'bg-amber-400',
    ItemCancelado: 'bg-red-500',
    PagamentoEstornado: 'bg-red-500',
    Cancelamento: 'bg-red-500',
    ItemAtribuido: 'bg-slate-400',
    PessoasDefinidas: 'bg-slate-400',
    PessoaRenomeada: 'bg-slate-400',
    TaxaServicoAlterada: 'bg-slate-400',
    MesaTrocada: 'bg-slate-400',
  };

  constructor() {
    effect(() => {
      if (this.visivel()) untracked(() => this.carregar());
    });
  }

  protected anulado(evento: EventoComanda): boolean {
    return TIPOS_ANULADOS.includes(evento.tipo);
  }

  private carregar(): void {
    this.carregando.set(true);

    this.comandas.historico(this.comandaId()).subscribe({
      next: (eventos) => {
        this.eventos.set(eventos);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.carregando.set(false);
        this.mensagens.add({
          severity: 'error',
          summary: 'Não foi possível carregar o histórico',
          detail: mensagemDeErro(erro),
          life: 5000,
        });
      },
    });
  }
}
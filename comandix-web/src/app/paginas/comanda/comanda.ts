import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ArrowLeft, LucideAngularModule, X } from 'lucide-angular';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { EMPTY, catchError, switchMap, timer } from 'rxjs';

import { ComandaDetalhe, ComandaService, ItemComanda, StatusItem } from '../../api/comanda.service';
import { mensagemDeErro } from '../../api/erros';
import { Mesa, SalaoService } from '../../api/salao.service';
import { AuthService } from '../../auth/auth.service';
import { tempoDecorrido } from '../../util/tempo';
import { HistoricoComanda } from './historico-comanda';
import { NovoPedido } from './novo-pedido';

type Severidade = 'success' | 'secondary' | 'warn' | 'danger';

const ORDEM_STATUS: Record<StatusItem, number> = {
  Pronto: 0,
  EmPreparo: 1,
  Pendente: 2,
  Entregue: 3,
  Cancelado: 4,
};

@Component({
  selector: 'app-comanda',
  imports: [
    CurrencyPipe,
    RouterLink,
    LucideAngularModule,
    ButtonModule,
    DialogModule,
    TagModule,
    NovoPedido,
    HistoricoComanda,
  ],
  template: `
    @if (comanda(); as c) {
      <div class="flex flex-col gap-6">
        <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div class="flex flex-col gap-1">
            <a
              routerLink="/app/mesas"
              class="inline-flex w-fit items-center gap-1 text-sm text-slate-500 hover:text-cyan-700 dark:text-slate-400 dark:hover:text-cyan-400"
            >
              <lucide-icon [img]="icones.voltar" [size]="16" />
              Mesas
            </a>
            <h1 class="text-2xl font-bold tracking-tight">Mesa {{ c.mesaNumero }}</h1>
            <p class="text-sm text-slate-500 dark:text-slate-400">
              {{ c.garcom }} · aberta {{ tempoAberta(c.abertaEm) }}
            </p>
          </div>

          <div class="flex flex-col gap-3 sm:items-end">
            <div class="sm:text-right">
              <span class="text-sm text-slate-500 dark:text-slate-400">Total</span>
              <p class="text-3xl font-bold tabular-nums">{{ c.total | currency }}</p>
              @if (c.taxaServico > 0) {
                <span class="text-xs text-slate-500 dark:text-slate-400">
                  inclui {{ c.taxaServico | currency }} de taxa de serviço
                </span>
              }
            </div>

            <div class="flex flex-wrap gap-2 sm:justify-end">
              @if (ehGerente()) {
                <p-button
                  label="Histórico da mesa"
                  severity="secondary"
                  [text]="true"
                  (onClick)="dialogoHistorico.set(true)"
                />
              }
              @if (podeEditar()) {
                <p-button label="Trocar mesa" severity="secondary" [outlined]="true" (onClick)="abrirTrocaMesa()" />
                <p-button label="Adicionar itens" (onClick)="dialogoPedido.set(true)" />
              }
            </div>
          </div>
        </div>

        @if (c.status !== 'Aberta') {
          <p
            class="rounded-lg border border-slate-200 bg-slate-100 px-3 py-2 text-sm text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300"
          >
            Esta comanda está {{ c.status === 'Fechada' ? 'fechada' : 'cancelada' }} e não pode mais ser alterada.
          </p>
        }

        <section class="flex flex-col gap-3">
          <h2 class="text-lg font-semibold">Itens</h2>

          @if (itensOrdenados().length === 0) {
            <div
              class="rounded-2xl border border-dashed border-slate-300 px-4 py-10 text-center text-slate-500 dark:border-slate-700 dark:text-slate-400"
            >
              Nenhum item pedido ainda.
              @if (podeEditar()) {
                Toque em "Adicionar itens" para começar.
              }
            </div>
          } @else {
            <ul
              class="flex flex-col divide-y divide-slate-200 rounded-2xl border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-800 dark:bg-slate-900"
            >
              @for (item of itensOrdenados(); track item.id) {
                <li
                  class="flex flex-wrap items-center gap-3 px-4 py-3"
                  [class.opacity-60]="item.status === 'Cancelado'"
                >
                  <div class="min-w-0 flex-1">
                    <p class="font-medium" [class.line-through]="item.status === 'Cancelado'">
                      {{ item.quantidade }}× {{ item.nomeProduto }}
                    </p>
                    @if (item.observacao) {
                      <p class="text-sm text-slate-500 dark:text-slate-400">{{ item.observacao }}</p>
                    }
                  </div>

                  <span class="text-sm tabular-nums" [class.line-through]="item.status === 'Cancelado'">
                    {{ item.subtotal | currency }}
                  </span>
                  <p-tag [value]="rotuloStatus[item.status]" [severity]="severidadeStatus[item.status]" />

                  @if (podeEditar()) {
                    @if (item.status === 'Pronto') {
                      <p-button
                        label="Entregar"
                        size="small"
                        severity="success"
                        [loading]="processando() === item.id"
                        (onClick)="entregar(item)"
                      />
                    }
                    @if (item.status !== 'Entregue' && item.status !== 'Cancelado') {
                      <button
                        type="button"
                        class="rounded-lg p-2 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950 dark:hover:text-red-400"
                        [attr.aria-label]="'Cancelar ' + item.nomeProduto"
                        title="Cancelar item"
                        (click)="cancelarItem(item)"
                      >
                        <lucide-icon [img]="icones.cancelar" [size]="16" />
                      </button>
                    }
                  }
                </li>
              }
            </ul>
          }
        </section>
      </div>
    } @else if (carregando()) {
      <p class="py-16 text-center text-slate-500 dark:text-slate-400">Carregando comanda...</p>
    } @else {
      <div class="flex flex-col items-center gap-3 py-16 text-center">
        <p class="text-slate-500 dark:text-slate-400">Comanda não encontrada.</p>
        <a routerLink="/app/mesas" class="font-medium text-cyan-700 hover:underline dark:text-cyan-400">
          Voltar para as mesas
        </a>
      </div>
    }

    <app-novo-pedido [comandaId]="id()" [(visivel)]="dialogoPedido" (enviado)="recarregar()" />

    @if (ehGerente()) {
      <app-historico-comanda [comandaId]="id()" [(visivel)]="dialogoHistorico" />
    }

    <p-dialog
      [visible]="dialogoTrocaMesa()"
      (visibleChange)="dialogoTrocaMesa.set($event)"
      header="Trocar de mesa"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '26rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [pt]="{ header: 'relative justify-center', headerActions: 'absolute right-4' }"
    >
      <div class="flex flex-col gap-4 text-center">
        <p class="text-slate-600 dark:text-slate-400">
          Escolha a mesa livre para onde a comanda vai.
        </p>

        @if (mesasLivres().length === 0) {
          <p class="text-sm text-slate-500 dark:text-slate-400">Nenhuma mesa livre no momento.</p>
        } @else {
          <div class="grid grid-cols-4 gap-2">
            @for (mesa of mesasLivres(); track mesa.id) {
              <button
                type="button"
                class="flex flex-col items-center rounded-xl border-2 border-emerald-200 bg-emerald-50/60 py-2 hover:border-emerald-400 disabled:opacity-50 dark:border-emerald-900 dark:bg-emerald-950/30 dark:hover:border-emerald-700"
                [disabled]="trocandoMesa()"
                (click)="trocarMesa(mesa)"
              >
                <span class="text-xl font-bold tabular-nums">{{ mesa.numero }}</span>
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ mesa.capacidade }} lug.</span>
              </button>
            }
          </div>
        }
      </div>
    </p-dialog>
  `,
})
export class Comanda {
  readonly id = input.required<string>();

  private readonly comandas = inject(ComandaService);
  private readonly salao = inject(SalaoService);
  private readonly auth = inject(AuthService);
  private readonly mensagens = inject(MessageService);
  private readonly confirmacao = inject(ConfirmationService);

  protected readonly icones = { voltar: ArrowLeft, cancelar: X };

  protected readonly rotuloStatus: Record<StatusItem, string> = {
    Pendente: 'Pendente',
    EmPreparo: 'Em preparo',
    Pronto: 'Pronto',
    Entregue: 'Entregue',
    Cancelado: 'Cancelado',
  };

  protected readonly severidadeStatus: Record<StatusItem, Severidade> = {
    Pendente: 'warn',
    EmPreparo: 'warn',
    Pronto: 'success',
    Entregue: 'secondary',
    Cancelado: 'danger',
  };

  protected readonly comanda = signal<ComandaDetalhe | null>(null);
  protected readonly carregando = signal(true);
  protected readonly agora = signal(Date.now());
  protected readonly processando = signal<string | null>(null);

  protected readonly dialogoPedido = signal(false);
  protected readonly dialogoHistorico = signal(false);
  protected readonly dialogoTrocaMesa = signal(false);
  protected readonly mesasLivres = signal<Mesa[]>([]);
  protected readonly trocandoMesa = signal(false);

  protected readonly ehGerente = computed(() => this.auth.cargo() === 'Gerente');

  protected readonly podeEditar = computed(() => {
    const cargo = this.auth.cargo();
    return this.comanda()?.status === 'Aberta' && (cargo === 'Gerente' || cargo === 'Garcom');
  });

  protected readonly itensOrdenados = computed(() =>
    [...(this.comanda()?.itens ?? [])].sort(
      (a, b) =>
        ORDEM_STATUS[a.status] - ORDEM_STATUS[b.status] ||
        Date.parse(a.pedidoEm) - Date.parse(b.pedidoEm)
    )
  );

  constructor() {
    toObservable(this.id)
      .pipe(
        switchMap((id) =>
          timer(0, 10_000).pipe(
            switchMap(() =>
              this.comandas.obter(id).pipe(
                catchError((erro) => {
                  this.carregando.set(false);
                  this.notificarErro(erro);
                  return EMPTY;
                })
              )
            )
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe((comanda) => this.atualizar(comanda));
  }

  protected tempoAberta(abertaEm: string): string {
    return tempoDecorrido(abertaEm, this.agora());
  }

  protected recarregar(): void {
    this.comandas.obter(this.id()).subscribe({
      next: (comanda) => this.atualizar(comanda),
      error: (erro) => this.notificarErro(erro),
    });
  }

  protected entregar(item: ItemComanda): void {
    this.processando.set(item.id);

    this.comandas.avancarItem(this.id(), item.id).subscribe({
      next: (comanda) => {
        this.processando.set(null);
        this.atualizar(comanda);
      },
      error: (erro) => {
        this.processando.set(null);
        this.notificarErro(erro);
      },
    });
  }

  protected cancelarItem(item: ItemComanda): void {
    this.confirmacao.confirm({
      header: 'Cancelar item',
      message: `Cancelar ${item.quantidade}× ${item.nomeProduto}?`,
      acceptLabel: 'Cancelar item',
      rejectLabel: 'Voltar',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () =>
        this.comandas.cancelarItem(this.id(), item.id).subscribe({
          next: (comanda) => {
            this.atualizar(comanda);
            this.mensagens.add({ severity: 'success', summary: 'Item cancelado.', life: 3000 });
          },
          error: (erro) => this.notificarErro(erro),
        }),
    });
  }

  protected abrirTrocaMesa(): void {
    this.mesasLivres.set([]);
    this.dialogoTrocaMesa.set(true);

    this.salao.listarMesas().subscribe({
      next: (mesas) => this.mesasLivres.set(mesas.filter((m) => m.status === 'Livre')),
      error: (erro) => this.notificarErro(erro),
    });
  }

  protected trocarMesa(mesa: Mesa): void {
    this.trocandoMesa.set(true);

    this.comandas.trocarMesa(this.id(), mesa.id).subscribe({
      next: (comanda) => {
        this.trocandoMesa.set(false);
        this.dialogoTrocaMesa.set(false);
        this.atualizar(comanda);
        this.mensagens.add({
          severity: 'success',
          summary: `Comanda movida para a mesa ${mesa.numero}.`,
          life: 3000,
        });
      },
      error: (erro) => {
        this.trocandoMesa.set(false);
        this.notificarErro(erro);
        this.abrirTrocaMesa();
      },
    });
  }

  private atualizar(comanda: ComandaDetalhe): void {
    this.comanda.set(comanda);
    this.agora.set(Date.now());
    this.carregando.set(false);
  }

  private notificarErro(erro: unknown): void {
    this.mensagens.add({
      severity: 'error',
      summary: 'Não foi possível concluir',
      detail: mensagemDeErro(erro),
      life: 5000,
    });
  }
}
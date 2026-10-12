import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { forkJoin, timer } from 'rxjs';

import { mensagemDeErro } from '../../api/erros';
import { ComandaResumo, Funcionario, Mesa, SalaoService } from '../../api/salao.service';
import { AuthService } from '../../auth/auth.service';

interface MesaNoMapa {
  mesa: Mesa;
  comanda: ComandaResumo | null;
}

const INTERVALO_ATUALIZACAO = 15_000;

@Component({
  selector: 'app-mesas',
  imports: [
    CurrencyPipe,
    FormsModule,
    ButtonModule,
    DialogModule,
    SelectModule,
    ToggleSwitchModule,
  ],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 class="text-2xl font-bold tracking-tight">Mesas</h1>
          <p class="text-sm text-slate-500 dark:text-slate-400">
            {{ contagem().livres }} {{ contagem().livres === 1 ? 'livre' : 'livres' }} ·
            {{ contagem().ocupadas }} {{ contagem().ocupadas === 1 ? 'ocupada' : 'ocupadas' }}
          </p>
        </div>

        @if (!ehGerente()) {
          <label for="so-minhas" class="flex cursor-pointer items-center gap-2 text-sm font-medium">
            <p-toggleswitch
              inputId="so-minhas"
              [ngModel]="soAsMinhas()"
              (ngModelChange)="soAsMinhas.set($event)"
            />
            Só as minhas
          </label>
        }
      </div>

      @if (carregando()) {
        <p class="py-16 text-center text-slate-500 dark:text-slate-400">Carregando mesas...</p>
      } @else if (mapa().length === 0) {
        <p class="py-16 text-center text-slate-500 dark:text-slate-400">
          {{ soAsMinhas() ? 'Você não tem mesas abertas no momento.' : 'Nenhuma mesa cadastrada.' }}
        </p>
      } @else {
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          @for (item of mapa(); track item.mesa.id) {
            <button
              type="button"
              class="flex min-h-36 flex-col gap-2 rounded-2xl border-2 p-4 text-left transition-colors"
              [class]="item.comanda ? cartaoOcupada : cartaoLivre"
              [attr.aria-label]="
                'Mesa ' + item.mesa.numero + (item.comanda ? ', ocupada' : ', livre')
              "
              (click)="selecionar(item)"
            >
              <div class="flex items-start justify-between gap-2">
                <span class="text-3xl font-bold tabular-nums">{{ item.mesa.numero }}</span>
                @if (item.comanda?.itensProntos; as prontos) {
                  <span
                    class="rounded-full bg-amber-400 px-2 py-0.5 text-xs font-bold whitespace-nowrap text-slate-950"
                  >
                    {{ prontos }} {{ prontos === 1 ? 'pronto' : 'prontos' }}
                  </span>
                }
              </div>

              @if (item.comanda; as comanda) {
                <div class="mt-auto flex flex-col gap-0.5 text-sm">
                  <span class="font-semibold tabular-nums">{{ comanda.total | currency }}</span>
                  <span class="truncate text-slate-500 dark:text-slate-400">
                    {{ comanda.garcom }} · {{ tempoAberta(comanda.abertaEm) }}
                  </span>
                </div>
              } @else {
                <div class="mt-auto flex flex-col gap-0.5 text-sm">
                  <span class="font-semibold text-emerald-700 dark:text-emerald-400">Livre</span>
                  <span class="text-slate-500 dark:text-slate-400">
                    {{ item.mesa.capacidade }} lugares
                  </span>
                </div>
              }
            </button>
          }
        </div>
      }
    </div>

    <p-dialog
      [visible]="!!mesaParaAbrir()"
      (visibleChange)="fecharDialogo($event)"
      [header]="'Abrir mesa ' + (mesaParaAbrir()?.numero ?? '')"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
    >
      <div class="flex flex-col gap-4">
        <p class="text-slate-600 dark:text-slate-400">
          @if (ehGerente()) {
            Escolha o garçom responsável pela mesa.
          } @else {
            A comanda será aberta no seu nome.
          }
        </p>

        @if (ehGerente()) {
          <p-select
            [options]="garcons()"
            optionLabel="nome"
            optionValue="id"
            placeholder="Selecione o garçom"
            appendTo="body"
            [fluid]="true"
            [ngModel]="garcomSelecionado()"
            (ngModelChange)="garcomSelecionado.set($event)"
          />
        }
      </div>

      <ng-template #footer>
        <p-button
          label="Cancelar"
          severity="secondary"
          [text]="true"
          (onClick)="fecharDialogo(false)"
        />
        <p-button
          label="Abrir mesa"
          [loading]="abrindo()"
          [disabled]="ehGerente() && !garcomSelecionado()"
          (onClick)="confirmarAbertura()"
        />
      </ng-template>
    </p-dialog>
  `,
})
export class Mesas {
  private readonly salao = inject(SalaoService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MessageService);

  protected readonly cartaoLivre =
    'border-emerald-200 bg-emerald-50/60 hover:border-emerald-400 dark:border-emerald-900 dark:bg-emerald-950/30 dark:hover:border-emerald-700';
  protected readonly cartaoOcupada =
    'border-cyan-300 bg-cyan-50/60 hover:border-cyan-500 dark:border-cyan-800 dark:bg-cyan-950/40 dark:hover:border-cyan-600';

  protected readonly ehGerente = computed(() => this.auth.cargo() === 'Gerente');

  protected readonly mesas = signal<Mesa[]>([]);
  protected readonly comandas = signal<ComandaResumo[]>([]);
  protected readonly carregando = signal(true);
  protected readonly agora = signal(Date.now());
  protected readonly soAsMinhas = signal(false);

  protected readonly mapa = computed<MesaNoMapa[]>(() => {
    const comandaPorMesa = new Map(this.comandas().map((c) => [c.mesaId, c]));
    const meuId = this.auth.sessao()?.funcionarioId;

    return this.mesas()
      .map((mesa) => ({ mesa, comanda: comandaPorMesa.get(mesa.id) ?? null }))
      .filter((item) => !this.soAsMinhas() || item.comanda?.garcomId === meuId);
  });

  protected readonly contagem = computed(() => {
    const ocupadas = this.mesas().filter((m) => m.status === 'Ocupada').length;
    return { livres: this.mesas().length - ocupadas, ocupadas };
  });

  protected readonly mesaParaAbrir = signal<Mesa | null>(null);
  protected readonly garcons = signal<Funcionario[]>([]);
  protected readonly garcomSelecionado = signal<string | null>(null);
  protected readonly abrindo = signal(false);

  constructor() {
    timer(0, INTERVALO_ATUALIZACAO)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.atualizar());
  }

  protected selecionar(item: MesaNoMapa): void {
    if (item.comanda) {
      this.router.navigate(['/app/comandas', item.comanda.id]);
      return;
    }

    this.garcomSelecionado.set(null);
    this.mesaParaAbrir.set(item.mesa);

    if (this.ehGerente() && this.garcons().length === 0) {
      this.salao.listarGarcons().subscribe({
        next: (garcons) => this.garcons.set(garcons),
        error: (erro) => this.notificarErro(erro),
      });
    }
  }

  protected fecharDialogo(visivel: boolean): void {
    if (!visivel) this.mesaParaAbrir.set(null);
  }

  protected confirmarAbertura(): void {
    const mesa = this.mesaParaAbrir();
    if (!mesa) return;

    this.abrindo.set(true);

    this.salao
      .abrirComanda(mesa.id, this.ehGerente() ? (this.garcomSelecionado() ?? undefined) : undefined)
      .subscribe({
        next: (comanda) => {
          this.abrindo.set(false);
          this.mesaParaAbrir.set(null);
          this.router.navigate(['/app/comandas', comanda.id]);
        },
        error: (erro) => {
          this.abrindo.set(false);
          this.mesaParaAbrir.set(null);
          this.notificarErro(erro);
          this.atualizar();
        },
      });
  }

  protected tempoAberta(abertaEm: string): string {
    const minutos = Math.floor((this.agora() - Date.parse(abertaEm)) / 60_000);

    if (minutos < 1) return 'agora';
    if (minutos < 60) return `há ${minutos} min`;

    return `há ${Math.floor(minutos / 60)} h ${minutos % 60} min`;
  }

  private atualizar(): void {
    forkJoin({
      mesas: this.salao.listarMesas(),
      comandas: this.salao.listarComandasAbertas(),
    }).subscribe({
      next: ({ mesas, comandas }) => {
        this.mesas.set(mesas);
        this.comandas.set(comandas);
        this.agora.set(Date.now());
        this.carregando.set(false);
      },
      error: (erro) => {
        this.notificarErro(erro);
        this.carregando.set(false);
      },
    });
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

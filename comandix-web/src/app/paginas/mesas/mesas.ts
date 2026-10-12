import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule, Pencil, Trash2 } from 'lucide-angular';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { Observable, forkJoin, timer } from 'rxjs';
import { tempoDecorrido } from '../../util/tempo';

import { mensagemDeErro } from '../../api/erros';
import { ComandaResumo, Funcionario, Mesa, MesaDados, SalaoService } from '../../api/salao.service';
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
    ReactiveFormsModule,
    LucideAngularModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
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

        @if (ehGerente()) {
          <div class="flex flex-wrap gap-2">
            @if (modoEdicao()) {
              <p-button label="Criar mesa" (onClick)="novaMesa()" />
              <p-button label="Concluir" severity="secondary" [outlined]="true" (onClick)="modoEdicao.set(false)" />
            } @else {
              <p-button label="Modo edição" severity="secondary" [outlined]="true" (onClick)="modoEdicao.set(true)" />
            }
          </div>
        } @else {
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

      @if (modoEdicao()) {
        <p
          class="rounded-lg border border-cyan-200 bg-cyan-50 px-3 py-2 text-sm text-cyan-800 dark:border-cyan-900 dark:bg-cyan-950/50 dark:text-cyan-300"
        >
          Modo edição: use o lápis para editar e a lixeira para excluir. Mesas ocupadas não podem ser alteradas.
        </p>
      }

      @if (carregando()) {
        <p class="py-16 text-center text-slate-500 dark:text-slate-400">Carregando mesas...</p>
      } @else if (mapa().length === 0) {
        <p class="py-16 text-center text-slate-500 dark:text-slate-400">
          {{ soAsMinhas() ? 'Você não tem mesas abertas no momento.' : 'Nenhuma mesa cadastrada.' }}
        </p>
      } @else {
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          @for (item of mapa(); track item.mesa.id) {
            <div class="relative">
              <button
                type="button"
                class="flex min-h-36 w-full flex-col items-center justify-center gap-1 rounded-2xl border-2 p-4 text-center transition-colors disabled:cursor-default"
                [class]="item.comanda ? cartaoOcupada : cartaoLivre"
                [disabled]="modoEdicao()"
                [attr.aria-label]="'Mesa ' + item.mesa.numero + (item.comanda ? ', ocupada' : ', livre')"
                (click)="selecionar(item)"
              >
                <span class="text-3xl font-bold tabular-nums">{{ item.mesa.numero }}</span>

                @if (item.comanda; as comanda) {
                  <span class="text-sm font-semibold text-cyan-700 dark:text-cyan-400">Ocupada</span>
                  <span class="text-sm text-slate-500 tabular-nums dark:text-slate-400">
                    {{ comanda.total | currency }}
                  </span>
                  <span class="max-w-full truncate text-sm text-slate-500 dark:text-slate-400">
                    {{ comanda.garcom }} · {{ tempoAberta(comanda.abertaEm) }}
                  </span>
                } @else {
                  <span class="text-sm font-semibold text-emerald-700 dark:text-emerald-400">Livre</span>
                  <span class="text-sm text-slate-500 dark:text-slate-400">
                    {{ item.mesa.capacidade }} lugares
                  </span>
                }
              </button>

              @if (!modoEdicao() && item.comanda?.itensProntos; as prontos) {
                <span
                  class="pointer-events-none absolute top-2 right-2 rounded-full bg-amber-400 px-2 py-0.5 text-xs font-bold whitespace-nowrap text-slate-950"
                >
                  {{ prontos }} {{ prontos === 1 ? 'pronto' : 'prontos' }}
                </span>
              }

              @if (modoEdicao()) {
                <div class="absolute top-2 right-2 flex gap-1">
                  <button
                    type="button"
                    [class]="botaoAcao"
                    [disabled]="!!item.comanda"
                    [title]="item.comanda ? 'Mesa ocupada não pode ser editada' : 'Editar'"
                    [attr.aria-label]="'Editar mesa ' + item.mesa.numero"
                    (click)="editarMesa(item.mesa)"
                  >
                    <lucide-icon [img]="icones.editar" [size]="16" />
                  </button>
                  <button
                    type="button"
                    [class]="botaoRemover"
                    [disabled]="!!item.comanda"
                    [title]="item.comanda ? 'Mesa ocupada não pode ser excluída' : 'Excluir'"
                    [attr.aria-label]="'Excluir mesa ' + item.mesa.numero"
                    (click)="removerMesa(item.mesa)"
                  >
                    <lucide-icon [img]="icones.remover" [size]="16" />
                  </button>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>

    <p-dialog
      [visible]="!!mesaParaAbrir()"
      (visibleChange)="fecharDialogoAbertura($event)"
      [header]="'Abrir mesa ' + (mesaParaAbrir()?.numero ?? '')"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [pt]="dialogoCentralizado"
    >
            <div class="flex flex-col items-center gap-4 text-center">
        <p class="text-slate-600 dark:text-slate-400">
          @if (ehGerente()) {
            Escolha o garçom responsável pela mesa.
          } @else {
            A comanda será aberta no seu nome.
          }
        </p>

        @if (ehGerente()) {
          <p-select
            class="w-full"
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
        <div class="flex w-full justify-center gap-2">
          <p-button label="Cancelar" severity="secondary" [text]="true" (onClick)="fecharDialogoAbertura(false)" />
          <p-button
            label="Abrir mesa"
            [loading]="abrindo()"
            [disabled]="ehGerente() && !garcomSelecionado()"
            (onClick)="confirmarAbertura()"
          />
        </div>
      </ng-template>
    </p-dialog>

    <p-dialog
      [visible]="dialogoMesa()"
      (visibleChange)="dialogoMesa.set($event)"
      [header]="mesaEmEdicao() ? 'Editar mesa ' + mesaEmEdicao()!.numero : 'Criar mesa'"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
    >
      <form [formGroup]="formMesa" class="grid grid-cols-2 gap-4 pt-1">
        <div class="flex flex-col gap-2">
          <label for="mesa-numero" class="text-sm font-medium">Número</label>
          <p-inputnumber
            inputId="mesa-numero"
            formControlName="numero"
            [useGrouping]="false"
            [min]="1"
            [max]="9999"
            [fluid]="true"
          />
          @if (formMesa.controls.numero.invalid && formMesa.controls.numero.touched) {
            <small class="text-red-600 dark:text-red-400">Entre 1 e 9999.</small>
          }
        </div>

        <div class="flex flex-col gap-2">
          <label for="mesa-capacidade" class="text-sm font-medium">Lugares</label>
          <p-inputnumber
            inputId="mesa-capacidade"
            formControlName="capacidade"
            [showButtons]="true"
            [min]="1"
            [max]="50"
            [fluid]="true"
          />
          @if (formMesa.controls.capacidade.invalid && formMesa.controls.capacidade.touched) {
            <small class="text-red-600 dark:text-red-400">Entre 1 e 50.</small>
          }
        </div>
      </form>

      <ng-template #footer>
        <p-button label="Cancelar" severity="secondary" [text]="true" (onClick)="dialogoMesa.set(false)" />
        <p-button label="Salvar" [loading]="salvando()" (onClick)="salvarMesa()" />
      </ng-template>
    </p-dialog>
  `,
})
export class Mesas {
  private readonly salao = inject(SalaoService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MessageService);
  private readonly confirmacao = inject(ConfirmationService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icones = { editar: Pencil, remover: Trash2 };

  protected readonly cartaoLivre =
    'border-emerald-200 bg-emerald-50/60 enabled:hover:border-emerald-400 dark:border-emerald-900 dark:bg-emerald-950/30 dark:enabled:hover:border-emerald-700';
  protected readonly cartaoOcupada =
    'border-cyan-300 bg-cyan-50/60 enabled:hover:border-cyan-500 dark:border-cyan-800 dark:bg-cyan-950/40 dark:enabled:hover:border-cyan-600';
  protected readonly botaoAcao =
    'rounded-lg bg-white/80 p-1.5 text-slate-600 shadow-sm hover:bg-white hover:text-slate-900 disabled:cursor-not-allowed disabled:opacity-40 dark:bg-slate-900/80 dark:text-slate-300 dark:hover:bg-slate-800';
  protected readonly botaoRemover =
    'rounded-lg bg-white/80 p-1.5 text-slate-600 shadow-sm hover:bg-red-50 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-40 dark:bg-slate-900/80 dark:text-slate-300 dark:hover:bg-red-950 dark:hover:text-red-400';
  protected readonly dialogoCentralizado = 
    {header: 'relative justify-center',headerActions: 'absolute right-4',};

  protected readonly ehGerente = computed(() => this.auth.cargo() === 'Gerente');

  protected readonly mesas = signal<Mesa[]>([]);
  protected readonly comandas = signal<ComandaResumo[]>([]);
  protected readonly carregando = signal(true);
  protected readonly agora = signal(Date.now());
  protected readonly soAsMinhas = signal(false);
  protected readonly modoEdicao = signal(false);

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

  protected readonly dialogoMesa = signal(false);
  protected readonly mesaEmEdicao = signal<Mesa | null>(null);
  protected readonly salvando = signal(false);
  protected readonly formMesa = this.fb.group({
    numero: this.fb.control<number | null>(null, [
      Validators.required,
      Validators.min(1),
      Validators.max(9999),
    ]),
    capacidade: this.fb.control<number | null>(4, [
      Validators.required,
      Validators.min(1),
      Validators.max(50),
    ]),
  });

  constructor() {
    timer(0, INTERVALO_ATUALIZACAO)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.atualizar());
  }

  protected selecionar(item: MesaNoMapa): void {
    if (this.modoEdicao()) return;

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

  protected fecharDialogoAbertura(visivel: boolean): void {
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

  protected novaMesa(): void {
    const proximoNumero = Math.max(0, ...this.mesas().map((m) => m.numero)) + 1;

    this.mesaEmEdicao.set(null);
    this.formMesa.reset({ numero: proximoNumero, capacidade: 4 });
    this.dialogoMesa.set(true);
  }

  protected editarMesa(mesa: Mesa): void {
    this.mesaEmEdicao.set(mesa);
    this.formMesa.reset({ numero: mesa.numero, capacidade: mesa.capacidade });
    this.dialogoMesa.set(true);
  }

  protected salvarMesa(): void {
    if (this.formMesa.invalid) {
      this.formMesa.markAllAsTouched();
      return;
    }

    const valores = this.formMesa.getRawValue();
    const dados: MesaDados = { numero: valores.numero!, capacidade: valores.capacidade! };
    const emEdicao = this.mesaEmEdicao();

    const requisicao: Observable<unknown> = emEdicao
      ? this.salao.atualizarMesa(emEdicao.id, dados)
      : this.salao.criarMesa(dados);

    this.executar(requisicao, emEdicao ? 'Mesa atualizada.' : 'Mesa criada.', () =>
      this.dialogoMesa.set(false)
    );
  }

  protected removerMesa(mesa: Mesa): void {
    this.confirmacao.confirm({
      header: 'Excluir mesa',
      message: `Excluir a mesa ${mesa.numero}?`,
      acceptLabel: 'Excluir',
      rejectLabel: 'Cancelar',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.executar(this.salao.removerMesa(mesa.id), 'Mesa excluída.'),
    });
  }

    protected tempoAberta(abertaEm: string): string {
    return tempoDecorrido(abertaEm, this.agora());
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

  private executar(requisicao: Observable<unknown>, mensagem: string, aoConcluir?: () => void): void {
    this.salvando.set(true);

    requisicao.subscribe({
      next: () => {
        this.salvando.set(false);
        aoConcluir?.();
        this.mensagens.add({ severity: 'success', summary: mensagem, life: 3000 });
        this.atualizar();
      },
      error: (erro) => {
        this.salvando.set(false);
        this.notificarErro(erro);
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
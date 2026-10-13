import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule, Minus, Pencil, Plus } from 'lucide-angular';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { Observable } from 'rxjs';

import {
  ComandaDetalhe,
  ComandaService,
  DivisaoPessoa,
  FormaPagamento,
  ItemComanda,
  PagamentoComanda,
} from '../../api/comanda.service';
import { mensagemDeErro } from '../../api/erros';
import { AuthService } from '../../auth/auth.service';

const CENTRALIZADO = { header: 'relative justify-center', headerActions: 'absolute right-4' };

@Component({
  selector: 'app-conta-comanda',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    LucideAngularModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    ToggleSwitchModule,
  ],
  template: `
    @let c = comanda();

    <div class="flex flex-col gap-6 pt-2">
      <section class="grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div [class]="cartao">
          <span class="text-sm text-slate-500 dark:text-slate-400">Consumo</span>
          <span class="text-xl font-semibold tabular-nums">{{ c.consumo | currency }}</span>
        </div>

        <div [class]="cartao">
          <div class="flex items-center justify-between gap-2">
            <span class="text-sm text-slate-500 dark:text-slate-400">Taxa de serviço</span>
            @if (podeEditar()) {
              <p-toggleswitch
                ariaLabel="Cobrar taxa de serviço"
                [ngModel]="c.cobrarTaxaServico"
                [disabled]="processando()"
                (ngModelChange)="alternarTaxa($event)"
              />
            }
          </div>
          <span class="text-xl font-semibold tabular-nums" [class.line-through]="!c.cobrarTaxaServico">
            {{ c.taxaServico | currency }}
          </span>
        </div>

        <div [class]="cartao">
          <span class="text-sm text-slate-500 dark:text-slate-400">Pago</span>
          <span class="text-xl font-semibold tabular-nums">{{ c.totalPago | currency }}</span>
        </div>

        <div [class]="cartao">
          <span class="text-sm text-slate-500 dark:text-slate-400">
            {{ c.saldo > 0 ? 'Falta pagar' : 'Conta' }}
          </span>
          @if (c.saldo > 0) {
            <span class="text-xl font-bold tabular-nums text-amber-600 dark:text-amber-400">
              {{ c.saldo | currency }}
            </span>
          } @else {
            <span class="text-xl font-bold text-emerald-600 dark:text-emerald-400">Paga</span>
          }
        </div>
      </section>

      <section class="flex flex-col gap-3">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-lg font-semibold">Divisão da conta</h2>
          @if (podeEditar()) {
            <div class="flex items-center gap-2">
              <span class="text-sm text-slate-500 dark:text-slate-400">Pessoas</span>
              <button
                type="button"
                [class]="botaoQuantidade"
                aria-label="Diminuir pessoas"
                [disabled]="c.divisao.length <= 1 || processando()"
                (click)="alterarPessoas(-1)"
              >
                <lucide-icon [img]="icones.menos" [size]="16" />
              </button>
              <span class="w-6 text-center font-semibold tabular-nums">{{ c.divisao.length }}</span>
              <button
                type="button"
                [class]="botaoQuantidade"
                aria-label="Aumentar pessoas"
                [disabled]="c.divisao.length >= 50 || processando()"
                (click)="alterarPessoas(1)"
              >
                <lucide-icon [img]="icones.mais" [size]="16" />
              </button>
            </div>
          }
        </div>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          @for (pessoa of c.divisao; track pessoa.pessoaId) {
            <div [class]="cartao">
              <div class="flex min-h-9 items-center justify-between gap-2">
                @if (editandoPessoa() === pessoa.pessoaId) {
                  <input
                    pInputText
                    class="w-full"
                    maxlength="60"
                    aria-label="Nome da pessoa"
                    [ngModel]="nomeEditado()"
                    (ngModelChange)="nomeEditado.set($event)"
                    (keydown.enter)="salvarNome()"
                    (keydown.escape)="editandoPessoa.set(null)"
                    (blur)="salvarNome()"
                  />
                } @else {
                  <span class="truncate font-semibold">{{ pessoa.nome }}</span>
                  @if (podeEditar()) {
                    <button
                      type="button"
                      [class]="botaoIcone"
                      [attr.aria-label]="'Renomear ' + pessoa.nome"
                      title="Renomear"
                      (click)="iniciarRenomear(pessoa)"
                    >
                      <lucide-icon [img]="icones.editar" [size]="14" />
                    </button>
                  }
                }
              </div>

              <dl class="grid grid-cols-2 gap-y-1 text-sm">
                <dt class="text-slate-500 dark:text-slate-400">Consumo</dt>
                <dd class="text-right tabular-nums">{{ pessoa.consumo | currency }}</dd>
                <dt class="text-slate-500 dark:text-slate-400">Taxa</dt>
                <dd class="text-right tabular-nums">{{ pessoa.taxaServico | currency }}</dd>
                <dt class="font-semibold">Total</dt>
                <dd class="text-right font-semibold tabular-nums">{{ pessoa.total | currency }}</dd>
                <dt class="text-slate-500 dark:text-slate-400">Pago</dt>
                <dd class="text-right tabular-nums">{{ pessoa.pago | currency }}</dd>
              </dl>

              @if (pessoa.saldo > 0 && c.saldo > 0) {
                @if (podeCobrar()) {
                  <p-button
                    size="small"
                    [label]="'Pagar ' + (pessoa.saldo | currency)"
                    [fluid]="true"
                    (onClick)="abrirPagamento(pessoa)"
                  />
                } @else {
                  <span class="text-sm font-medium text-amber-600 dark:text-amber-400">
                    Falta {{ pessoa.saldo | currency }}
                  </span>
                }
              } @else {
                <span class="text-sm font-medium text-emerald-600 dark:text-emerald-400">Pago</span>
              }
            </div>
          }
        </div>
      </section>

      @if (c.divisao.length > 1 && itensAtivos().length > 0) {
        <section class="flex flex-col gap-3">
          <div>
            <h2 class="text-lg font-semibold">Quem consumiu cada item</h2>
            <p class="text-sm text-slate-500 dark:text-slate-400">
              Itens marcados como "Todos" são divididos igualmente entre a mesa.
            </p>
          </div>

          <ul [class]="lista">
            @for (item of itensAtivos(); track item.id) {
              <li class="flex flex-col gap-2 px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
                <span class="font-medium">
                  {{ item.quantidade }}× {{ item.nomeProduto }}
                  <span class="text-sm font-normal text-slate-500 tabular-nums dark:text-slate-400">
                    · {{ item.subtotal | currency }}
                  </span>
                </span>
                <div class="flex flex-wrap gap-1.5">
                  <button
                    type="button"
                    [class]="item.pagantesIds.length === 0 ? chipAtivo : chipInativo"
                    [disabled]="!podeEditar() || processando()"
                    (click)="dividirEntreTodos(item)"
                  >
                    Todos
                  </button>
                  @for (pessoa of c.divisao; track pessoa.pessoaId) {
                    <button
                      type="button"
                      [class]="item.pagantesIds.includes(pessoa.pessoaId) ? chipAtivo : chipInativo"
                      [disabled]="!podeEditar() || processando()"
                      (click)="alternarPagante(item, pessoa.pessoaId)"
                    >
                      {{ pessoa.nome }}
                    </button>
                  }
                </div>
              </li>
            }
          </ul>
        </section>
      }

      <section class="flex flex-col gap-3">
        <div class="flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-lg font-semibold">Pagamentos</h2>
          @if (podeCobrar() && c.saldo > 0) {
            <p-button label="Pagar conta inteira" [outlined]="true" (onClick)="abrirPagamento(null)" />
          }
        </div>

        @if (c.pagamentos.length === 0) {
          <div
            class="rounded-2xl border border-dashed border-slate-300 px-4 py-8 text-center text-slate-500 dark:border-slate-700 dark:text-slate-400"
          >
            Nenhum pagamento registrado.
          </div>
        } @else {
          <ul [class]="lista">
            @for (pagamento of c.pagamentos; track pagamento.id) {
              <li class="flex items-center gap-3 px-4 py-3" [class.opacity-60]="pagamento.estornado">
                <div class="min-w-0 flex-1">
                  <p class="font-medium" [class.line-through]="pagamento.estornado">
                    {{ rotuloForma[pagamento.forma] }}
                    @if (nomeDaPessoa(pagamento.pessoaId); as nome) {
                      <span class="font-normal text-slate-500 dark:text-slate-400">· {{ nome }}</span>
                    }
                  </p>
                  <p class="text-sm text-slate-500 dark:text-slate-400">
                    {{ pagamento.registradoEm | date: 'HH:mm' }}
                    @if (pagamento.troco > 0) {
                      · recebido {{ pagamento.valorRecebido | currency }}, troco {{ pagamento.troco | currency }}
                    }
                  </p>
                  @if (pagamento.estornado) {
                    <p class="text-sm text-red-600 dark:text-red-400">Estornado: {{ pagamento.motivoEstorno }}</p>
                  }
                </div>

                <span class="font-semibold tabular-nums" [class.line-through]="pagamento.estornado">
                  {{ pagamento.valor | currency }}
                </span>

                @if (podeEstornar() && !pagamento.estornado) {
                  <p-button
                    label="Estornar"
                    size="small"
                    severity="danger"
                    [text]="true"
                    (onClick)="abrirEstorno(pagamento)"
                  />
                }
              </li>
            }
          </ul>
        }
      </section>

      @if (podeCobrar()) {
        <div
          class="flex flex-col gap-3 rounded-2xl border border-slate-200 bg-white p-4 sm:flex-row sm:items-center sm:justify-between dark:border-slate-800 dark:bg-slate-900"
        >
          <p class="text-sm text-slate-600 dark:text-slate-400">
            @if (itensAtivos().length === 0) {
              A comanda ainda não tem itens.
            } @else if (itensPendentes()) {
              Ainda há itens que não foram entregues.
            } @else if (c.saldo > 0) {
              Falta receber {{ c.saldo | currency }}.
            } @else if (c.saldo < 0) {
              Os pagamentos passaram do total em {{ -c.saldo | currency }}. Estorne o excedente.
            } @else {
              Tudo entregue e pago. A mesa já pode ser fechada.
            }
          </p>
          <p-button
            label="Fechar mesa"
            severity="success"
            [disabled]="!podeFechar() || processando()"
            (onClick)="fechar()"
          />
        </div>
      }
    </div>

    <p-dialog
      [visible]="dialogoPagamento()"
      (visibleChange)="dialogoPagamento.set($event)"
      [header]="pagamentoPessoa() ? 'Pagamento de ' + pagamentoPessoa()!.nome : 'Pagamento da mesa'"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '26rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [pt]="centralizado"
    >
      <div class="flex flex-col gap-4">
        <div class="grid grid-cols-2 gap-2">
          @for (opcao of formas; track opcao) {
            <button
              type="button"
              class="rounded-xl border-2 px-3 py-3 text-sm font-semibold transition-colors"
              [class]="forma() === opcao ? formaAtiva : formaInativa"
              (click)="forma.set(opcao)"
            >
              {{ rotuloForma[opcao] }}
            </button>
          }
        </div>

        <div class="flex flex-col gap-2">
          <label for="pagamento-valor" class="text-sm font-medium">Valor</label>
          <p-inputnumber
            inputId="pagamento-valor"
            mode="currency"
            currency="BRL"
            locale="pt-BR"
            [min]="0"
            [fluid]="true"
            [ngModel]="valor()"
            (ngModelChange)="valor.set($event)"
          />
        </div>

        @if (forma() === 'Dinheiro') {
          <div class="flex flex-col gap-2">
            <label for="pagamento-recebido" class="text-sm font-medium">Valor recebido</label>
            <p-inputnumber
              inputId="pagamento-recebido"
              mode="currency"
              currency="BRL"
              locale="pt-BR"
              [min]="0"
              [fluid]="true"
              [ngModel]="valorRecebido()"
              (ngModelChange)="valorRecebido.set($event)"
            />
            @if (recebidoInsuficiente()) {
              <small class="text-red-600 dark:text-red-400">
                O valor recebido é menor que o valor do pagamento.
              </small>
            }
          </div>

          <div class="flex items-center justify-between rounded-lg bg-slate-100 px-3 py-2 dark:bg-slate-800">
            <span class="text-sm font-medium">Troco</span>
            <span class="text-lg font-bold tabular-nums">{{ troco() | currency }}</span>
          </div>
        }
      </div>

      <ng-template #footer>
        <div class="flex w-full justify-center gap-2">
          <p-button label="Cancelar" severity="secondary" [text]="true" (onClick)="dialogoPagamento.set(false)" />
          <p-button
            label="Registrar pagamento"
            [loading]="processando()"
            [disabled]="!valor() || recebidoInsuficiente()"
            (onClick)="confirmarPagamento()"
          />
        </div>
      </ng-template>
    </p-dialog>

    <p-dialog
      [visible]="!!pagamentoParaEstornar()"
      (visibleChange)="!$event && pagamentoParaEstornar.set(null)"
      header="Estornar pagamento"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '26rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [pt]="centralizado"
    >
      @if (pagamentoParaEstornar(); as pagamento) {
        <div class="flex flex-col gap-4">
          <p class="text-center text-slate-600 dark:text-slate-400">
            {{ rotuloForma[pagamento.forma] }} de {{ pagamento.valor | currency }}
          </p>
          <div class="flex flex-col gap-2">
            <label for="estorno-motivo" class="text-sm font-medium">Motivo</label>
            <input
              pInputText
              id="estorno-motivo"
              maxlength="200"
              class="w-full"
              placeholder="Ex.: lançado na forma errada"
              [ngModel]="motivoEstorno()"
              (ngModelChange)="motivoEstorno.set($event)"
            />
          </div>
        </div>
      }

      <ng-template #footer>
        <div class="flex w-full justify-center gap-2">
          <p-button label="Voltar" severity="secondary" [text]="true" (onClick)="pagamentoParaEstornar.set(null)" />
          <p-button
            label="Estornar"
            severity="danger"
            [loading]="processando()"
            [disabled]="!motivoEstorno().trim()"
            (onClick)="confirmarEstorno()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class ContaComanda {
  readonly comanda = input.required<ComandaDetalhe>();
  readonly atualizada = output<ComandaDetalhe>();

  private readonly comandas = inject(ComandaService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly mensagens = inject(MessageService);
  private readonly confirmacao = inject(ConfirmationService);

  protected readonly icones = { mais: Plus, menos: Minus, editar: Pencil };
  protected readonly centralizado = CENTRALIZADO;
  protected readonly formas: FormaPagamento[] = ['Pix', 'CartaoDebito', 'CartaoCredito', 'Dinheiro'];
  protected readonly rotuloForma: Record<FormaPagamento, string> = {
    Pix: 'Pix',
    CartaoDebito: 'Débito',
    CartaoCredito: 'Crédito',
    Dinheiro: 'Dinheiro',
  };

  protected readonly cartao =
    'flex flex-col gap-2 rounded-2xl border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900';
  protected readonly lista =
    'flex flex-col divide-y divide-slate-200 rounded-2xl border border-slate-200 bg-white dark:divide-slate-800 dark:border-slate-800 dark:bg-slate-900';
  protected readonly chipAtivo =
    'rounded-full px-3 py-1 text-sm font-medium bg-cyan-700 text-white disabled:cursor-default dark:bg-cyan-400 dark:text-slate-950';
  protected readonly chipInativo =
    'rounded-full px-3 py-1 text-sm font-medium bg-slate-100 text-slate-600 hover:bg-slate-200 disabled:cursor-default disabled:hover:bg-slate-100 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700';
  protected readonly botaoQuantidade =
    'flex size-8 items-center justify-center rounded-full border border-slate-300 hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-40 dark:border-slate-700 dark:hover:bg-slate-800';
  protected readonly botaoIcone =
    'rounded-lg p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-900 dark:hover:bg-slate-800 dark:hover:text-slate-100';
  protected readonly formaAtiva =
    'border-cyan-600 bg-cyan-50 text-cyan-800 dark:border-cyan-400 dark:bg-cyan-950 dark:text-cyan-300';
  protected readonly formaInativa =
    'border-slate-200 hover:border-slate-400 dark:border-slate-700 dark:hover:border-slate-500';

  protected readonly processando = signal(false);

  protected readonly podeEditar = computed(
    () => this.comanda().status === 'Aberta' && this.cargoEntre('Gerente', 'Garcom')
  );
  protected readonly podeCobrar = computed(
    () => this.comanda().status === 'Aberta' && this.cargoEntre('Gerente', 'Garcom', 'Caixa')
  );
  protected readonly podeEstornar = computed(
    () => this.comanda().status === 'Aberta' && this.cargoEntre('Gerente', 'Caixa')
  );

  protected readonly itensAtivos = computed(() =>
    this.comanda().itens.filter((i) => i.status !== 'Cancelado')
  );
  protected readonly itensPendentes = computed(() =>
    this.itensAtivos().some((i) => i.status !== 'Entregue')
  );
  protected readonly podeFechar = computed(
    () => this.itensAtivos().length > 0 && !this.itensPendentes() && this.comanda().saldo === 0
  );

  protected readonly editandoPessoa = signal<string | null>(null);
  protected readonly nomeEditado = signal('');

  protected readonly dialogoPagamento = signal(false);
  protected readonly pagamentoPessoa = signal<DivisaoPessoa | null>(null);
  protected readonly forma = signal<FormaPagamento>('Pix');
  protected readonly valor = signal<number | null>(null);
  protected readonly valorRecebido = signal<number | null>(null);

  protected readonly recebidoInsuficiente = computed(() => {
    const recebido = this.valorRecebido();
    const valor = this.valor();
    return this.forma() === 'Dinheiro' && recebido !== null && valor !== null && recebido < valor;
  });

  protected readonly troco = computed(() => {
    const recebido = this.valorRecebido();
    const valor = this.valor();
    return recebido !== null && valor !== null ? Math.max(0, recebido - valor) : 0;
  });

  protected readonly pagamentoParaEstornar = signal<PagamentoComanda | null>(null);
  protected readonly motivoEstorno = signal('');

  protected nomeDaPessoa(pessoaId: string | null): string | null {
    return this.comanda().divisao.find((p) => p.pessoaId === pessoaId)?.nome ?? null;
  }

  protected alterarPessoas(diferenca: number): void {
    this.executar(
      this.comandas.definirPessoas(this.comanda().id, this.comanda().divisao.length + diferenca)
    );
  }

  protected iniciarRenomear(pessoa: DivisaoPessoa): void {
    this.nomeEditado.set(pessoa.nome);
    this.editandoPessoa.set(pessoa.pessoaId);
  }

  protected salvarNome(): void {
    const pessoaId = this.editandoPessoa();
    if (!pessoaId) return;

    this.editandoPessoa.set(null);
    const nome = this.nomeEditado().trim();
    const atual = this.nomeDaPessoa(pessoaId);
    if (!nome || nome === atual) return;

    this.executar(this.comandas.renomearPessoa(this.comanda().id, pessoaId, nome));
  }

  protected dividirEntreTodos(item: ItemComanda): void {
    if (item.pagantesIds.length === 0) return;
    this.executar(this.comandas.atribuirItem(this.comanda().id, item.id, []));
  }

  protected alternarPagante(item: ItemComanda, pessoaId: string): void {
    const pagantes = item.pagantesIds.includes(pessoaId)
      ? item.pagantesIds.filter((id) => id !== pessoaId)
      : [...item.pagantesIds, pessoaId];

    this.executar(this.comandas.atribuirItem(this.comanda().id, item.id, pagantes));
  }

  protected alternarTaxa(cobrar: boolean): void {
    this.executar(
      this.comandas.definirTaxaServico(this.comanda().id, cobrar),
      cobrar ? 'Taxa de serviço incluída.' : 'Taxa de serviço retirada.'
    );
  }

  protected abrirPagamento(pessoa: DivisaoPessoa | null): void {
    const saldo = pessoa ? pessoa.saldo : this.comanda().saldo;

    this.pagamentoPessoa.set(pessoa);
    this.forma.set('Pix');
    this.valor.set(Math.max(0, saldo));
    this.valorRecebido.set(null);
    this.dialogoPagamento.set(true);
  }

  protected confirmarPagamento(): void {
    const valor = this.valor();
    if (!valor || this.recebidoInsuficiente()) return;

    const emDinheiro = this.forma() === 'Dinheiro';

    this.executar(
      this.comandas.registrarPagamento(this.comanda().id, {
        forma: this.forma(),
        valor,
        valorRecebido: emDinheiro ? (this.valorRecebido() ?? valor) : undefined,
        pessoaId: this.pagamentoPessoa()?.pessoaId,
      }),
      'Pagamento registrado.',
      () => this.dialogoPagamento.set(false)
    );
  }

  protected abrirEstorno(pagamento: PagamentoComanda): void {
    this.motivoEstorno.set('');
    this.pagamentoParaEstornar.set(pagamento);
  }

  protected confirmarEstorno(): void {
    const pagamento = this.pagamentoParaEstornar();
    const motivo = this.motivoEstorno().trim();
    if (!pagamento || !motivo) return;

    this.executar(
      this.comandas.estornarPagamento(this.comanda().id, pagamento.id, motivo),
      'Pagamento estornado.',
      () => this.pagamentoParaEstornar.set(null)
    );
  }

  protected fechar(): void {
    const c = this.comanda();

    this.confirmacao.confirm({
      header: `Fechar a mesa ${c.mesaNumero}`,
      message: 'A comanda será encerrada, a mesa ficará livre e a nota fiscal (simulada) será emitida.',
      acceptLabel: 'Fechar mesa',
      rejectLabel: 'Voltar',
      acceptButtonProps: { severity: 'success' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () =>
        this.executar(this.comandas.fechar(c.id), `Mesa ${c.mesaNumero} fechada.`, () =>
          this.router.navigate(['/app/mesas'])
        ),
    });
  }

  private cargoEntre(...cargos: string[]): boolean {
    const cargo = this.auth.cargo();
    return !!cargo && cargos.includes(cargo);
  }

  private executar(
    requisicao: Observable<ComandaDetalhe>,
    mensagem?: string,
    aoConcluir?: () => void
  ): void {
    this.processando.set(true);

    requisicao.subscribe({
      next: (comanda) => {
        this.processando.set(false);
        this.atualizada.emit(comanda);
        aoConcluir?.();
        if (mensagem) this.mensagens.add({ severity: 'success', summary: mensagem, life: 3000 });
      },
      error: (erro) => {
        this.processando.set(false);
        this.mensagens.add({
          severity: 'error',
          summary: 'Não foi possível concluir',
          detail: mensagemDeErro(erro),
          life: 5000,
        });
      },
    });
  }
}
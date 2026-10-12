import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, input, model, output, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Minus, Plus } from 'lucide-angular';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { concatMap, from, tap } from 'rxjs';

import { CardapioService, Produto } from '../../api/cardapio.service';
import { ComandaService } from '../../api/comanda.service';
import { mensagemDeErro } from '../../api/erros';

interface LinhaPedido {
  produto: Produto;
  quantidade: number;
  observacao: string;
}

function normalizar(texto: string): string {
  return texto.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
}

@Component({
  selector: 'app-novo-pedido',
  imports: [CurrencyPipe, FormsModule, LucideAngularModule, ButtonModule, DialogModule, InputTextModule],
  template: `
    <p-dialog
      [visible]="visivel()"
      (visibleChange)="visivel.set($event)"
      header="Adicionar itens"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '40rem' }"
      [breakpoints]="{ '640px': '100vw' }"
      [pt]="{ header: 'relative justify-center', headerActions: 'absolute right-4' }"
    >
      <div class="flex flex-col gap-4">
        <input
          pInputText
          type="search"
          placeholder="Buscar no cardápio"
          class="w-full"
          [ngModel]="busca()"
          (ngModelChange)="busca.set($event)"
        />

        <div class="flex gap-2 overflow-x-auto pb-1">
          <button
            type="button"
            class="shrink-0 rounded-full px-3 py-1 text-sm font-medium transition-colors"
            [class]="categoria() === null ? chipAtivo : chipInativo"
            (click)="categoria.set(null)"
          >
            Todas
          </button>
          @for (cat of categorias(); track cat.id) {
            <button
              type="button"
              class="shrink-0 rounded-full px-3 py-1 text-sm font-medium transition-colors"
              [class]="categoria() === cat.id ? chipAtivo : chipInativo"
              (click)="categoria.set(cat.id)"
            >
              {{ cat.nome }}
            </button>
          }
        </div>

        <ul class="flex max-h-[50vh] flex-col divide-y divide-slate-200 overflow-y-auto dark:divide-slate-800">
          @if (carregando()) {
            <li class="py-10 text-center text-slate-500 dark:text-slate-400">Carregando cardápio...</li>
          }
          @for (produto of produtosFiltrados(); track produto.id) {
            <li class="flex flex-col gap-2 py-3">
              <div class="flex items-center gap-3">
                <div class="min-w-0 flex-1">
                  <p class="font-medium">{{ produto.nome }}</p>
                  @if (produto.descricao) {
                    <p class="truncate text-sm text-slate-500 dark:text-slate-400">{{ produto.descricao }}</p>
                  }
                  <p class="text-sm tabular-nums text-slate-600 dark:text-slate-300">
                    {{ produto.preco | currency }}
                  </p>
                </div>

                @if (quantidadeDe(produto.id); as quantidade) {
                  <div class="flex items-center gap-2">
                    <button type="button" [class]="botaoQuantidade" aria-label="Diminuir" (click)="diminuir(produto)">
                      <lucide-icon [img]="icones.menos" [size]="16" />
                    </button>
                    <span class="w-6 text-center font-semibold tabular-nums">{{ quantidade }}</span>
                    <button type="button" [class]="botaoQuantidade" aria-label="Aumentar" (click)="adicionar(produto)">
                      <lucide-icon [img]="icones.mais" [size]="16" />
                    </button>
                  </div>
                } @else {
                  <p-button label="Adicionar" size="small" [outlined]="true" (onClick)="adicionar(produto)" />
                }
              </div>

              @if (quantidadeDe(produto.id)) {
                <input
                  pInputText
                  placeholder="Observação (ex.: sem cebola)"
                  maxlength="250"
                  class="w-full text-sm"
                  [ngModel]="observacaoDe(produto.id)"
                  (ngModelChange)="definirObservacao(produto.id, $event)"
                />
              }
            </li>
          } @empty {
            @if (!carregando()) {
              <li class="py-10 text-center text-slate-500 dark:text-slate-400">Nenhum produto encontrado.</li>
            }
          }
        </ul>
      </div>

      <ng-template #footer>
        <div class="flex w-full items-center justify-between gap-3">
          <span class="text-sm text-slate-600 dark:text-slate-400">
            {{ totalItens() }} {{ totalItens() === 1 ? 'item' : 'itens' }} ·
            <strong class="tabular-nums">{{ totalValor() | currency }}</strong>
          </span>
          <p-button
            label="Enviar para a cozinha"
            [loading]="enviando()"
            [disabled]="totalItens() === 0"
            (onClick)="enviar()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class NovoPedido {
  readonly comandaId = input.required<string>();
  readonly visivel = model(false);
  readonly enviado = output<void>();

  private readonly cardapio = inject(CardapioService);
  private readonly comandas = inject(ComandaService);
  private readonly mensagens = inject(MessageService);

  protected readonly icones = { mais: Plus, menos: Minus };
  protected readonly chipAtivo = 'bg-cyan-700 text-white dark:bg-cyan-400 dark:text-slate-950';
  protected readonly chipInativo =
    'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700';
  protected readonly botaoQuantidade =
    'flex size-8 items-center justify-center rounded-full border border-slate-300 hover:bg-slate-100 dark:border-slate-700 dark:hover:bg-slate-800';

  protected readonly produtos = signal<Produto[]>([]);
  protected readonly carregando = signal(false);
  protected readonly busca = signal('');
  protected readonly categoria = signal<string | null>(null);
  protected readonly linhas = signal<LinhaPedido[]>([]);
  protected readonly enviando = signal(false);

  protected readonly categorias = computed(() => {
    const unicas = new Map(this.produtos().map((p) => [p.categoriaId, p.categoria]));
    return [...unicas].map(([id, nome]) => ({ id, nome }));
  });

  protected readonly produtosFiltrados = computed(() => {
    const termo = normalizar(this.busca().trim());
    const categoria = this.categoria();

    return this.produtos().filter(
      (p) => (!categoria || p.categoriaId === categoria) && (!termo || normalizar(p.nome).includes(termo))
    );
  });

  protected readonly totalItens = computed(() =>
    this.linhas().reduce((soma, linha) => soma + linha.quantidade, 0)
  );

  protected readonly totalValor = computed(() =>
    this.linhas().reduce((soma, linha) => soma + linha.quantidade * linha.produto.preco, 0)
  );

  constructor() {
    effect(() => {
      if (this.visivel() && this.produtos().length === 0) untracked(() => this.carregarProdutos());
    });
  }

  protected quantidadeDe(produtoId: string): number {
    return this.linhas().find((l) => l.produto.id === produtoId)?.quantidade ?? 0;
  }

  protected observacaoDe(produtoId: string): string {
    return this.linhas().find((l) => l.produto.id === produtoId)?.observacao ?? '';
  }

  protected adicionar(produto: Produto): void {
    this.linhas.update((linhas) =>
      linhas.some((l) => l.produto.id === produto.id)
        ? linhas.map((l) => (l.produto.id === produto.id ? { ...l, quantidade: l.quantidade + 1 } : l))
        : [...linhas, { produto, quantidade: 1, observacao: '' }]
    );
  }

  protected diminuir(produto: Produto): void {
    this.linhas.update((linhas) =>
      linhas
        .map((l) => (l.produto.id === produto.id ? { ...l, quantidade: l.quantidade - 1 } : l))
        .filter((l) => l.quantidade > 0)
    );
  }

  protected definirObservacao(produtoId: string, observacao: string): void {
    this.linhas.update((linhas) =>
      linhas.map((l) => (l.produto.id === produtoId ? { ...l, observacao } : l))
    );
  }

  protected enviar(): void {
    const linhas = this.linhas();
    if (linhas.length === 0) return;

    this.enviando.set(true);

    from(linhas)
      .pipe(
        concatMap((linha) =>
          this.comandas
            .adicionarItem(
              this.comandaId(),
              linha.produto.id,
              linha.quantidade,
              linha.observacao.trim() || null
            )
            .pipe(
              tap(() =>
                this.linhas.update((atuais) => atuais.filter((l) => l.produto.id !== linha.produto.id))
              )
            )
        )
      )
      .subscribe({
        complete: () => {
          this.enviando.set(false);
          this.busca.set('');
          this.mensagens.add({ severity: 'success', summary: 'Pedido enviado para a cozinha.', life: 3000 });
          this.visivel.set(false);
          this.enviado.emit();
        },
        error: (erro) => {
          this.enviando.set(false);
          this.mensagens.add({
            severity: 'error',
            summary: 'Parte do pedido não foi enviada',
            detail: mensagemDeErro(erro),
            life: 5000,
          });
          this.enviado.emit();
        },
      });
  }

  private carregarProdutos(): void {
    this.carregando.set(true);

    this.cardapio.listarProdutos().subscribe({
      next: (produtos) => {
        this.produtos.set(produtos.filter((p) => p.disponivel));
        this.carregando.set(false);
      },
      error: (erro) => {
        this.carregando.set(false);
        this.mensagens.add({
          severity: 'error',
          summary: 'Não foi possível carregar o cardápio',
          detail: mensagemDeErro(erro),
          life: 5000,
        });
      },
    });
  }
}
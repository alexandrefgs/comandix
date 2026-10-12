import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideAngularModule, Pencil, Trash2 } from 'lucide-angular';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TabsModule } from 'primeng/tabs';
import { TextareaModule } from 'primeng/textarea';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { Observable, forkJoin } from 'rxjs';

import { Categoria, CardapioService, Produto, ProdutoDados } from '../../api/cardapio.service';
import { mensagemDeErro } from '../../api/erros';

@Component({
  selector: 'app-cardapio',
  imports: [
    CurrencyPipe,
    FormsModule,
    ReactiveFormsModule,
    LucideAngularModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    InputTextModule,
    SelectModule,
    TableModule,
    TabsModule,
    TextareaModule,
    ToggleSwitchModule,
  ],
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold tracking-tight">Cardápio</h1>
        <p class="text-sm text-slate-500 dark:text-slate-400">
          Categorias e produtos oferecidos no restaurante.
        </p>
      </div>

      <p-tabs value="produtos">
        <p-tablist>
          <p-tab value="produtos">Produtos</p-tab>
          <p-tab value="categorias">Categorias</p-tab>
        </p-tablist>

        <p-tabpanels>
          <p-tabpanel value="produtos">
            <div class="flex flex-col gap-4 pt-2">
              <div class="flex flex-wrap items-center justify-between gap-3">
                <div class="flex flex-wrap gap-2">
                  <button
                    type="button"
                    class="rounded-full px-3 py-1 text-sm font-medium transition-colors"
                    [class]="filtroCategoria() === null ? chipAtivo : chipInativo"
                    (click)="filtroCategoria.set(null)"
                  >
                    Todas
                  </button>
                  @for (categoria of categorias(); track categoria.id) {
                    <button
                      type="button"
                      class="rounded-full px-3 py-1 text-sm font-medium transition-colors"
                      [class]="filtroCategoria() === categoria.id ? chipAtivo : chipInativo"
                      (click)="filtroCategoria.set(categoria.id)"
                    >
                      {{ categoria.nome }}
                    </button>
                  }
                </div>
                <p-button
                  label="Novo produto"
                  [disabled]="categorias().length === 0"
                  (onClick)="novoProduto()"
                />
              </div>

              <p-table [value]="produtosFiltrados()" [loading]="carregando()" dataKey="id">
                <ng-template #header>
                  <tr>
                    <th>Produto</th>
                    <th>Categoria</th>
                    <th class="text-right">Preço</th>
                    <th>Disponível</th>
                    <th></th>
                  </tr>
                </ng-template>
                <ng-template #body let-produto>
                  <tr>
                    <td>
                      <div class="font-medium">{{ produto.nome }}</div>
                      @if (produto.descricao) {
                        <div class="text-sm text-slate-500 dark:text-slate-400">
                          {{ produto.descricao }}
                        </div>
                      }
                    </td>
                    <td>{{ produto.categoria }}</td>
                    <td class="text-right tabular-nums">{{ produto.preco | currency }}</td>
                    <td>
                      <p-toggleswitch
                        [ngModel]="produto.disponivel"
                        (ngModelChange)="alternarDisponibilidade(produto, $event)"
                        [ariaLabel]="'Disponibilidade de ' + produto.nome"
                      />
                    </td>
                    <td>
                      <div class="flex justify-end gap-1">
                        <button
                          type="button"
                          [class]="botaoAcao"
                          [attr.aria-label]="'Editar ' + produto.nome"
                          title="Editar"
                          (click)="editarProduto(produto)"
                        >
                          <lucide-icon [img]="icones.editar" [size]="16" />
                        </button>
                        <button
                          type="button"
                          [class]="botaoRemover"
                          [attr.aria-label]="'Remover ' + produto.nome"
                          title="Remover"
                          (click)="removerProduto(produto)"
                        >
                          <lucide-icon [img]="icones.remover" [size]="16" />
                        </button>
                      </div>
                    </td>
                  </tr>
                </ng-template>
                <ng-template #emptymessage>
                  <tr>
                    <td colspan="5" class="py-10 text-center text-slate-500 dark:text-slate-400">
                      @if (categorias().length === 0) {
                        Cadastre uma categoria antes de criar produtos.
                      } @else {
                        Nenhum produto por aqui ainda.
                      }
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            </div>
          </p-tabpanel>

          <p-tabpanel value="categorias">
            <div class="flex flex-col gap-4 pt-2">
              <div class="flex justify-end">
                <p-button label="Nova categoria" (onClick)="novaCategoria()" />
              </div>

              <p-table [value]="categorias()" [loading]="carregando()" dataKey="id">
                <ng-template #header>
                  <tr>
                    <th>Categoria</th>
                    <th class="text-right">Produtos</th>
                    <th></th>
                  </tr>
                </ng-template>
                <ng-template #body let-categoria>
                  <tr>
                    <td class="font-medium">{{ categoria.nome }}</td>
                    <td class="text-right tabular-nums">{{ quantidadePorCategoria()[categoria.id] ?? 0 }}</td>
                    <td>
                      <div class="flex justify-end gap-1">
                        <button
                          type="button"
                          [class]="botaoAcao"
                          [attr.aria-label]="'Editar ' + categoria.nome"
                          title="Editar"
                          (click)="editarCategoria(categoria)"
                        >
                          <lucide-icon [img]="icones.editar" [size]="16" />
                        </button>
                        <button
                          type="button"
                          [class]="botaoRemover"
                          [attr.aria-label]="'Remover ' + categoria.nome"
                          title="Remover"
                          (click)="removerCategoria(categoria)"
                        >
                          <lucide-icon [img]="icones.remover" [size]="16" />
                        </button>
                      </div>
                    </td>
                  </tr>
                </ng-template>
                <ng-template #emptymessage>
                  <tr>
                    <td colspan="3" class="py-10 text-center text-slate-500 dark:text-slate-400">
                      Nenhuma categoria cadastrada.
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            </div>
          </p-tabpanel>
        </p-tabpanels>
      </p-tabs>
    </div>

    <p-dialog
      [visible]="dialogoProduto()"
      (visibleChange)="dialogoProduto.set($event)"
      [header]="produtoEmEdicao() ? 'Editar produto' : 'Novo produto'"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '32rem' }"
      [breakpoints]="{ '640px': '95vw' }"
    >
      <form [formGroup]="formProduto" class="flex flex-col gap-4 pt-1">
        <div class="flex flex-col gap-2">
          <label for="produto-nome" class="text-sm font-medium">Nome</label>
          <input pInputText id="produto-nome" formControlName="nome" class="w-full" />
          @if (formProduto.controls.nome.invalid && formProduto.controls.nome.touched) {
            <small class="text-red-600 dark:text-red-400">Informe o nome do produto.</small>
          }
        </div>

        <div class="flex flex-col gap-2">
          <label for="produto-descricao" class="text-sm font-medium">Descrição</label>
          <textarea pTextarea id="produto-descricao" formControlName="descricao" rows="3" class="w-full"></textarea>
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <div class="flex flex-col gap-2">
            <label for="produto-preco" class="text-sm font-medium">Preço</label>
            <p-inputnumber
              inputId="produto-preco"
              formControlName="preco"
              mode="currency"
              currency="BRL"
              locale="pt-BR"
              [min]="0"
              [fluid]="true"
            />
            @if (formProduto.controls.preco.invalid && formProduto.controls.preco.touched) {
              <small class="text-red-600 dark:text-red-400">Informe um preço maior que zero.</small>
            }
          </div>

          <div class="flex flex-col gap-2">
            <label for="produto-categoria" class="text-sm font-medium">Categoria</label>
            <p-select
              inputId="produto-categoria"
              formControlName="categoriaId"
              [options]="categorias()"
              optionLabel="nome"
              optionValue="id"
              placeholder="Selecione"
              appendTo="body"
              [fluid]="true"
            />
            @if (formProduto.controls.categoriaId.invalid && formProduto.controls.categoriaId.touched) {
              <small class="text-red-600 dark:text-red-400">Escolha uma categoria.</small>
            }
          </div>
        </div>
      </form>

      <ng-template #footer>
        <p-button label="Cancelar" severity="secondary" [text]="true" (onClick)="dialogoProduto.set(false)" />
        <p-button label="Salvar" [loading]="salvando()" (onClick)="salvarProduto()" />
      </ng-template>
    </p-dialog>

    <p-dialog
      [visible]="dialogoCategoria()"
      (visibleChange)="dialogoCategoria.set($event)"
      [header]="categoriaEmEdicao() ? 'Editar categoria' : 'Nova categoria'"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
    >
      <form [formGroup]="formCategoria" class="flex flex-col gap-2 pt-1" (ngSubmit)="salvarCategoria()">
        <label for="categoria-nome" class="text-sm font-medium">Nome</label>
        <input pInputText id="categoria-nome" formControlName="nome" class="w-full" />
        @if (formCategoria.controls.nome.invalid && formCategoria.controls.nome.touched) {
          <small class="text-red-600 dark:text-red-400">Informe o nome da categoria.</small>
        }
      </form>

      <ng-template #footer>
        <p-button label="Cancelar" severity="secondary" [text]="true" (onClick)="dialogoCategoria.set(false)" />
        <p-button label="Salvar" [loading]="salvando()" (onClick)="salvarCategoria()" />
      </ng-template>
    </p-dialog>
  `,
})
export class Cardapio {
  private readonly cardapio = inject(CardapioService);
  private readonly mensagens = inject(MessageService);
  private readonly confirmacao = inject(ConfirmationService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly icones = { editar: Pencil, remover: Trash2 };

  protected readonly chipAtivo = 'bg-cyan-700 text-white dark:bg-cyan-400 dark:text-slate-950';
  protected readonly chipInativo =
    'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700';
  protected readonly botaoAcao =
    'rounded-lg p-2 text-slate-500 hover:bg-slate-100 hover:text-slate-900 dark:hover:bg-slate-800 dark:hover:text-slate-100';
  protected readonly botaoRemover =
    'rounded-lg p-2 text-slate-500 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-950 dark:hover:text-red-400';

  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly produtos = signal<Produto[]>([]);
  protected readonly carregando = signal(true);
  protected readonly salvando = signal(false);
  protected readonly filtroCategoria = signal<string | null>(null);

  protected readonly produtosFiltrados = computed(() => {
    const filtro = this.filtroCategoria();
    return filtro ? this.produtos().filter((p) => p.categoriaId === filtro) : this.produtos();
  });

  protected readonly quantidadePorCategoria = computed(() =>
    this.produtos().reduce<Record<string, number>>((contagem, produto) => {
      contagem[produto.categoriaId] = (contagem[produto.categoriaId] ?? 0) + 1;
      return contagem;
    }, {})
  );

  protected readonly dialogoProduto = signal(false);
  protected readonly produtoEmEdicao = signal<Produto | null>(null);
  protected readonly formProduto = this.fb.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(500)],
    preco: this.fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    categoriaId: ['', Validators.required],
  });

  protected readonly dialogoCategoria = signal(false);
  protected readonly categoriaEmEdicao = signal<Categoria | null>(null);
  protected readonly formCategoria = this.fb.group({
    nome: ['', [Validators.required, Validators.maxLength(100)]],
  });

  constructor() {
    this.carregar();
  }

  protected novoProduto(): void {
    this.produtoEmEdicao.set(null);
    this.formProduto.reset({ categoriaId: this.filtroCategoria() ?? '' });
    this.dialogoProduto.set(true);
  }

  protected editarProduto(produto: Produto): void {
    this.produtoEmEdicao.set(produto);
    this.formProduto.reset({
      nome: produto.nome,
      descricao: produto.descricao ?? '',
      preco: produto.preco,
      categoriaId: produto.categoriaId,
    });
    this.dialogoProduto.set(true);
  }

  protected salvarProduto(): void {
    if (this.formProduto.invalid) {
      this.formProduto.markAllAsTouched();
      return;
    }

    const valores = this.formProduto.getRawValue();
    const dados: ProdutoDados = {
      nome: valores.nome.trim(),
      descricao: valores.descricao.trim() || null,
      preco: valores.preco!,
      categoriaId: valores.categoriaId,
    };

    const emEdicao = this.produtoEmEdicao();
    const requisicao: Observable<unknown> = emEdicao
      ? this.cardapio.atualizarProduto(emEdicao.id, dados)
      : this.cardapio.criarProduto(dados);

    this.salvar(requisicao, emEdicao ? 'Produto atualizado.' : 'Produto criado.', () =>
      this.dialogoProduto.set(false)
    );
  }

  protected alternarDisponibilidade(produto: Produto, disponivel: boolean): void {
    this.cardapio.alterarDisponibilidade(produto.id, disponivel).subscribe({
      next: () =>
        this.produtos.update((lista) =>
          lista.map((p) => (p.id === produto.id ? { ...p, disponivel } : p))
        ),
      error: (erro) => {
        this.notificarErro(erro);
        this.carregar();
      },
    });
  }

  protected removerProduto(produto: Produto): void {
    this.confirmacao.confirm({
      header: 'Remover produto',
      message: `Remover "${produto.nome}" do cardápio?`,
      acceptLabel: 'Remover',
      rejectLabel: 'Cancelar',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.salvar(this.cardapio.removerProduto(produto.id), 'Produto removido.'),
    });
  }

  protected novaCategoria(): void {
    this.categoriaEmEdicao.set(null);
    this.formCategoria.reset();
    this.dialogoCategoria.set(true);
  }

  protected editarCategoria(categoria: Categoria): void {
    this.categoriaEmEdicao.set(categoria);
    this.formCategoria.reset({ nome: categoria.nome });
    this.dialogoCategoria.set(true);
  }

  protected salvarCategoria(): void {
    if (this.formCategoria.invalid) {
      this.formCategoria.markAllAsTouched();
      return;
    }

    const nome = this.formCategoria.getRawValue().nome.trim();
    const emEdicao = this.categoriaEmEdicao();
    const requisicao: Observable<unknown> = emEdicao
      ? this.cardapio.atualizarCategoria(emEdicao.id, nome)
      : this.cardapio.criarCategoria(nome);

    this.salvar(requisicao, emEdicao ? 'Categoria atualizada.' : 'Categoria criada.', () =>
      this.dialogoCategoria.set(false)
    );
  }

  protected removerCategoria(categoria: Categoria): void {
    this.confirmacao.confirm({
      header: 'Remover categoria',
      message: `Remover a categoria "${categoria.nome}"?`,
      acceptLabel: 'Remover',
      rejectLabel: 'Cancelar',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', text: true },
      accept: () => this.salvar(this.cardapio.removerCategoria(categoria.id), 'Categoria removida.'),
    });
  }

  private carregar(): void {
    this.carregando.set(true);

    forkJoin({
      categorias: this.cardapio.listarCategorias(),
      produtos: this.cardapio.listarProdutos(),
    }).subscribe({
      next: ({ categorias, produtos }) => {
        this.categorias.set(categorias);
        this.produtos.set(produtos);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.notificarErro(erro);
        this.carregando.set(false);
      },
    });
  }

  private salvar(requisicao: Observable<unknown>, mensagem: string, aoConcluir?: () => void): void {
    this.salvando.set(true);

    requisicao.subscribe({
      next: () => {
        this.salvando.set(false);
        aoConcluir?.();
        this.mensagens.add({ severity: 'success', summary: mensagem, life: 3000 });
        this.carregar();
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
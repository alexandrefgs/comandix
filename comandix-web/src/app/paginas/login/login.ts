import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

import { AuthService } from '../../auth/auth.service';
import { rotaInicial } from '../../auth/rotas-por-cargo';
import { definirIndexacao } from '../../seo/indexacao';
import { SeletorTema } from '../../tema/seletor-tema';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, ButtonModule, InputTextModule, SeletorTema],
  template: `
    <div class="relative flex min-h-dvh items-center justify-center px-4 py-10 sm:px-6 lg:px-10">
      <div class="absolute top-4 right-4 sm:right-6 lg:right-10">
        <app-seletor-tema />
      </div>

      <div class="w-full max-w-sm">
        <div class="mb-8 flex flex-col items-center gap-3 text-center">
          <a routerLink="/" aria-label="Voltar para a página inicial">
            <img src="favicon.svg" alt="" width="48" height="48" />
          </a>
          <h1 class="text-2xl font-bold tracking-tight">
            Entrar no Comand<span class="text-cyan-700 dark:text-cyan-400">ix</span>
          </h1>
          <p class="text-sm text-slate-500 dark:text-slate-400">Acesso para gerente e caixa.</p>
        </div>

        <form
          [formGroup]="form"
          (ngSubmit)="entrar()"
          class="flex flex-col gap-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900"
        >
          @if (sessaoExpirada) {
            <div
              class="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950/50 dark:text-amber-300"
            >
              Sua sessão expirou. Entre novamente.
            </div>
          }

          @if (erro()) {
            <div
              role="alert"
              class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300"
            >
              {{ erro() }}
            </div>
          }

          <div class="flex flex-col gap-2">
            <label for="usuario" class="text-sm font-medium">Usuário</label>
            <input
              pInputText
              id="usuario"
              formControlName="usuario"
              autocomplete="username"
              class="w-full"
            />
            @if (form.controls.usuario.invalid && form.controls.usuario.touched) {
              <small class="text-red-600 dark:text-red-400">Informe o usuário.</small>
            }
          </div>

          <div class="flex flex-col gap-2">
            <label for="senha" class="text-sm font-medium">Senha</label>
            <input
              pInputText
              id="senha"
              type="password"
              formControlName="senha"
              autocomplete="current-password"
              class="w-full"
            />
            @if (form.controls.senha.invalid && form.controls.senha.touched) {
              <small class="text-red-600 dark:text-red-400">Informe a senha.</small>
            }
          </div>

          <p-button type="submit" label="Entrar" [loading]="carregando()" [fluid]="true" />
        </form>

        <p class="mt-6 text-center text-xs text-slate-500 dark:text-slate-400">
          Garçom ou cozinha? O acesso por QR Code chega em breve.
        </p>
      </div>
    </div>
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly sessaoExpirada = inject(ActivatedRoute).snapshot.queryParamMap.has('expirada');
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    usuario: ['', Validators.required],
    senha: ['', Validators.required],
  });

  constructor() {
    definirIndexacao(false);
  }

  protected entrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    const { usuario, senha } = this.form.getRawValue();

    this.auth.login(usuario, senha).subscribe({
      next: (sessao) => this.router.navigateByUrl(rotaInicial(sessao.cargo)),
      error: (erro: HttpErrorResponse) => {
        this.erro.set(
          erro.status === 401
            ? (erro.error?.detail ?? 'Usuário ou senha inválidos.')
            : 'Não foi possível conectar ao servidor. Tente novamente.'
        );
        this.carregando.set(false);
      },
    });
  }
}
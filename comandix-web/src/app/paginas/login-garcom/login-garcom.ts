import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, PLATFORM_ID, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputOtpModule } from 'primeng/inputotp';
import { InputTextModule } from 'primeng/inputtext';

import { mensagemDeErro } from '../../api/erros';
import { AuthService, Sessao } from '../../auth/auth.service';
import { rotaInicial } from '../../auth/rotas-por-cargo';
import { definirIndexacao } from '../../seo/indexacao';
import { SeletorTema } from '../../tema/seletor-tema';

type Etapa = 'codigo' | 'pin' | 'criarPin' | 'bloqueado';

@Component({
  selector: 'app-login-garcom',
  imports: [FormsModule, RouterLink, ButtonModule, InputOtpModule, InputTextModule, SeletorTema],
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
            @switch (etapa()) {
              @case ('pin') { Olá, {{ nome() }}! }
              @case ('criarPin') { Primeiro acesso }
              @default { Entrar com o crachá }
            }
          </h1>
          <p class="text-sm text-slate-500 dark:text-slate-400">
            @switch (etapa()) {
              @case ('pin') { Digite seu PIN de 4 dígitos. }
              @case ('criarPin') { {{ nome() }}, crie um PIN de 4 dígitos para entrar. }
              @case ('bloqueado') { Acesso bloqueado. }
              @default { Acesso para garçom e cozinha. }
            }
          </p>
        </div>

        <div
          class="flex flex-col gap-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900"
        >
          @if (erro()) {
            <div
              role="alert"
              class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300"
            >
              {{ erro() }}
            </div>
          }

          @switch (etapa()) {
            @case ('codigo') {
              <form class="flex flex-col gap-5" (ngSubmit)="identificar()">
                <div class="flex flex-col gap-2">
                  <label for="codigo" class="text-sm font-medium">Código do crachá</label>
                  <input
                    pInputText
                    id="codigo"
                    name="codigo"
                    autocomplete="off"
                    class="w-full"
                    [ngModel]="codigo()"
                    (ngModelChange)="codigo.set($event)"
                  />
                  <small class="text-slate-500 dark:text-slate-400">
                    Leia o QR Code do crachá com a câmera do celular, ou cole o código aqui.
                  </small>
                </div>
                <p-button type="submit" label="Continuar" [loading]="carregando()" [fluid]="true" />
              </form>
            }

            @case ('pin') {
              <div class="flex flex-col items-center gap-5">
                <p-inputotp
                  [length]="4"
                  [integerOnly]="true"
                  [mask]="true"
                  [ngModel]="pin()"
                  (ngModelChange)="digitarPin($event)"
                />
                <p-button label="Entrar" [loading]="carregando()" [fluid]="true" (onClick)="entrar()" />
              </div>
            }

            @case ('criarPin') {
              <div class="flex flex-col items-center gap-5">
                <div class="flex flex-col items-center gap-2">
                  <span class="text-sm font-medium">Novo PIN</span>
                  <p-inputotp
                    [length]="4"
                    [integerOnly]="true"
                    [mask]="true"
                    [ngModel]="pin()"
                    (ngModelChange)="pin.set($event ?? '')"
                  />
                </div>
                <div class="flex flex-col items-center gap-2">
                  <span class="text-sm font-medium">Confirme o PIN</span>
                  <p-inputotp
                    [length]="4"
                    [integerOnly]="true"
                    [mask]="true"
                    [ngModel]="confirmacaoPin()"
                    (ngModelChange)="confirmacaoPin.set($event ?? '')"
                  />
                </div>
                <p-button
                  label="Criar PIN e entrar"
                  [loading]="carregando()"
                  [fluid]="true"
                  (onClick)="criarPin()"
                />
              </div>
            }

            @case ('bloqueado') {
              <div
                class="rounded-lg border border-red-200 bg-red-50 px-3 py-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/50 dark:text-red-300"
              >
                Seu acesso foi bloqueado após várias tentativas de PIN incorretas. Procure o gerente
                para desbloquear.
              </div>
            }
          }

          @if (etapa() !== 'codigo') {
            <button
              type="button"
              class="text-sm text-slate-500 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-100"
              (click)="voltar()"
            >
              Não é você? Usar outro crachá
            </button>
          }
        </div>

        <p class="mt-6 text-center text-xs text-slate-500 dark:text-slate-400">
          Gerente ou caixa?
          <a routerLink="/login" class="font-medium text-cyan-700 hover:underline dark:text-cyan-400">
            Entrar com usuário e senha
          </a>
        </p>
      </div>
    </div>
  `,
})
export class LoginGarcom {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly etapa = signal<Etapa>('codigo');
  protected readonly codigo = signal('');
  protected readonly nome = signal('');
  protected readonly pin = signal('');
  protected readonly confirmacaoPin = signal('');
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  constructor() {
    definirIndexacao(false);

    const fragmento = this.route.snapshot.fragment;
    const codigoDoLink = fragmento ? new URLSearchParams(fragmento).get('codigo') : null;

    if (codigoDoLink && isPlatformBrowser(inject(PLATFORM_ID))) {
      this.codigo.set(codigoDoLink);
      this.router.navigate([], { relativeTo: this.route, replaceUrl: true });
      this.identificar();
    }
  }

  protected identificar(): void {
    const codigo = this.codigo().trim();
    if (!codigo) {
      this.erro.set('Informe o código do crachá.');
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    this.auth.identificarQrCode(codigo).subscribe({
      next: (identificacao) => {
        this.carregando.set(false);
        this.nome.set(identificacao.nome);
        this.pin.set('');
        this.confirmacaoPin.set('');
        this.etapa.set(
          identificacao.acessoBloqueado ? 'bloqueado' : identificacao.possuiPin ? 'pin' : 'criarPin'
        );
      },
      error: (erro) => {
        this.carregando.set(false);
        this.erro.set(mensagemDeErro(erro));
      },
    });
  }

  protected digitarPin(valor: string | null): void {
    this.pin.set(valor ?? '');
    if (this.pin().length === 4) this.entrar();
  }

  protected entrar(): void {
    if (this.pin().length !== 4 || this.carregando()) return;

    this.carregando.set(true);
    this.erro.set(null);

    this.auth.loginQrCode(this.codigo().trim(), this.pin()).subscribe({
      next: (sessao) => this.irParaAreaInicial(sessao),
      error: (erro: HttpErrorResponse) => this.tratarErroDePin(erro),
    });
  }

  protected criarPin(): void {
    if (this.pin().length !== 4) {
      this.erro.set('O PIN deve ter 4 dígitos.');
      return;
    }

    if (this.pin() !== this.confirmacaoPin()) {
      this.erro.set('Os PINs não conferem. Digite novamente.');
      this.confirmacaoPin.set('');
      return;
    }

    this.carregando.set(true);
    this.erro.set(null);

    this.auth.definirPin(this.codigo().trim(), this.pin()).subscribe({
      next: (sessao) => this.irParaAreaInicial(sessao),
      error: (erro: HttpErrorResponse) => this.tratarErroDePin(erro),
    });
  }

  protected voltar(): void {
    this.etapa.set('codigo');
    this.codigo.set('');
    this.pin.set('');
    this.confirmacaoPin.set('');
    this.erro.set(null);
  }

  private irParaAreaInicial(sessao: Sessao): void {
    this.router.navigateByUrl(rotaInicial(sessao.cargo));
  }

  private tratarErroDePin(erro: HttpErrorResponse): void {
    this.carregando.set(false);
    this.pin.set('');
    this.confirmacaoPin.set('');

    if (erro.status === 403) {
      this.etapa.set('bloqueado');
      return;
    }

    if (erro.status === 409) {
      this.etapa.set('criarPin');
      return;
    }

    this.erro.set(mensagemDeErro(erro));
  }
}
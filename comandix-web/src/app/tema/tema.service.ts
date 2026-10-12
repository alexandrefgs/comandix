import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, effect, inject, signal } from '@angular/core';

export type Tema = 'claro' | 'escuro' | 'sistema';

const CHAVE = 'comandix-tema';
const TEMAS: Tema[] = ['claro', 'escuro', 'sistema'];

@Injectable({ providedIn: 'root' })
export class TemaService {
  private readonly document = inject(DOCUMENT);
  private readonly noNavegador = isPlatformBrowser(inject(PLATFORM_ID));
  private midiaEscura?: MediaQueryList;

  readonly tema = signal<Tema>('sistema');

  constructor() {
    if (!this.noNavegador) return;

    const salvo = localStorage.getItem(CHAVE) as Tema | null;
    if (salvo && TEMAS.includes(salvo)) this.tema.set(salvo);

    this.midiaEscura = window.matchMedia('(prefers-color-scheme: dark)');
    this.midiaEscura.addEventListener('change', () => this.aplicar());

    effect(() => {
      localStorage.setItem(CHAVE, this.tema());
      this.aplicar();
    });
  }

  definir(tema: Tema): void {
    this.tema.set(tema);
  }

  private aplicar(): void {
    const escuro =
      this.tema() === 'escuro' || (this.tema() === 'sistema' && !!this.midiaEscura?.matches);

    this.document.documentElement.classList.toggle('app-dark', escuro);
  }
}
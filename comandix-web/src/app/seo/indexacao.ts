import { inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';

export function definirIndexacao(indexar: boolean): void {
  inject(Meta).updateTag({
    name: 'robots',
    content: indexar ? 'index, follow' : 'noindex, nofollow',
  });
}
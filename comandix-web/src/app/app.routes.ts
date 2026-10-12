import { Routes } from '@angular/router';

import { Inicio } from './paginas/inicio/inicio';

export const routes: Routes = [
  { path: '', component: Inicio, title: 'Comandix: sistema de comandas para restaurantes' },
  { path: '**', redirectTo: '' },
];
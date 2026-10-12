import { Routes } from '@angular/router';

import { autenticadoGuard, visitanteGuard } from './auth/auth.guards';
import { Inicio } from './paginas/inicio/inicio';

export const routes: Routes = [
  { path: '', component: Inicio, title: 'Comandix: sistema de comandas para restaurantes' },
  {
    path: 'login',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./paginas/login/login').then((m) => m.Login),
    title: 'Entrar | Comandix',
  },
  {
    path: 'app',
    canActivate: [autenticadoGuard],
    loadComponent: () => import('./paginas/painel/painel').then((m) => m.Painel),
    title: 'Painel | Comandix',
  },
  { path: '**', redirectTo: '' },
];
import { inject } from '@angular/core';
import { Routes } from '@angular/router';

import { autenticadoGuard, cargoGuard, visitanteGuard } from './auth/auth.guards';
import { AuthService } from './auth/auth.service';
import { rotaInicial } from './auth/rotas-por-cargo';
import { ITENS_MENU } from './layout/menu';
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
    path: 'entrar/garcom',
    canActivate: [visitanteGuard],
    loadComponent: () => import('./paginas/login-garcom/login-garcom').then((m) => m.LoginGarcom),
    title: 'Entrar com o crachá | Comandix',
  },
  {
    path: 'app',
    canActivate: [autenticadoGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: () => rotaInicial(inject(AuthService).cargo()),
      },
      ...ITENS_MENU.map((item) => ({
        path: item.caminho,
        canActivate: [cargoGuard],
        data: { cargos: item.cargos, titulo: item.rotulo },
        loadComponent: item.carregar,
        title: `${item.rotulo} | Comandix`,
      })),
      {
        path: 'comandas/:id',
        canActivate: [cargoGuard],
        data: { cargos: ['Gerente', 'Garcom', 'Caixa'], titulo: 'Comanda' },
        loadComponent: () => import('./paginas/em-breve/em-breve').then((m) => m.EmBreve),
        title: 'Comanda | Comandix',
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

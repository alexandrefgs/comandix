import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService, Cargo } from './auth.service';
import { rotaInicial } from './rotas-por-cargo';

export const autenticadoGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? true : inject(Router).createUrlTree(['/login']);
};

export const visitanteGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? inject(Router).createUrlTree([rotaInicial(auth.cargo())]) : true;
};

export const cargoGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const permitidos = route.data['cargos'] as Cargo[] | undefined;
  const cargo = auth.cargo();

  if (!permitidos || (cargo && permitidos.includes(cargo))) return true;

  return inject(Router).createUrlTree([rotaInicial(cargo)]);
};
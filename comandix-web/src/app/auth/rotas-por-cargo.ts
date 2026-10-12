import { Cargo } from './auth.service';

export function rotaInicial(cargo: Cargo | null): string {
  switch (cargo) {
    default:
      return '/app';
  }
}
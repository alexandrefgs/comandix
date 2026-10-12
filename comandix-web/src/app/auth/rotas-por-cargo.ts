import { Cargo } from './auth.service';

const ROTA_INICIAL: Record<Cargo, string> = {
  Gerente: '/app/painel',
  Garcom: '/app/mesas',
  Cozinha: '/app/cozinha',
  Caixa: '/app/caixa',
};

export const ROTULO_CARGO: Record<Cargo, string> = {
  Gerente: 'Gerente',
  Garcom: 'Garçom',
  Cozinha: 'Cozinha',
  Caixa: 'Caixa',
};

export function rotaInicial(cargo: Cargo | null): string {
  return cargo ? ROTA_INICIAL[cargo] : '/login';
}
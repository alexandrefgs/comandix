import { Type } from '@angular/core';
import {
  BookOpen,
  ChefHat,
  Coins,
  Gauge,
  LayoutGrid,
  LucideIconData,
  TrendingUp,
  Users,
  Wallet,
} from 'lucide-angular';

import { Cargo } from '../auth/auth.service';

export interface ItemMenu {
  rotulo: string;
  caminho: string;
  icone: LucideIconData;
  cargos: Cargo[];
  carregar: () => Promise<Type<unknown>>;
}

const emBreve = () => import('../paginas/em-breve/em-breve').then((m) => m.EmBreve);

export const ITENS_MENU: ItemMenu[] = [
  {
    rotulo: 'Painel',
    caminho: 'painel',
    icone: Gauge,
    cargos: ['Gerente'],
    carregar: () => import('../paginas/painel/painel').then((m) => m.Painel),
  },
  {
    rotulo: 'Mesas',
    caminho: 'mesas',
    icone: LayoutGrid,
    cargos: ['Gerente', 'Garcom'],
    carregar: () => import('../paginas/mesas/mesas').then((m) => m.Mesas),
  },
  {
    rotulo: 'Cozinha',
    caminho: 'cozinha',
    icone: ChefHat,
    cargos: ['Gerente', 'Cozinha'],
    carregar: emBreve,
  },
  {
    rotulo: 'Caixa',
    caminho: 'caixa',
    icone: Wallet,
    cargos: ['Gerente', 'Caixa'],
    carregar: emBreve,
  },
  {
    rotulo: 'Cardápio',
    caminho: 'cardapio',
    icone: BookOpen,
    cargos: ['Gerente'],
    carregar: () => import('../paginas/cardapio/cardapio').then((m) => m.Cardapio),
  },
  {
    rotulo: 'Funcionários',
    caminho: 'funcionarios',
    icone: Users,
    cargos: ['Gerente'],
    carregar: emBreve,
  },
  {
    rotulo: 'Relatórios',
    caminho: 'relatorios',
    icone: TrendingUp,
    cargos: ['Gerente'],
    carregar: emBreve,
  },
  {
    rotulo: 'Minha taxa',
    caminho: 'minha-taxa',
    icone: Coins,
    cargos: ['Garcom'],
    carregar: emBreve,
  },
];

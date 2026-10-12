import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const slate = {
  0: '#ffffff',
  50: '{slate.50}',
  100: '{slate.100}',
  200: '{slate.200}',
  300: '{slate.300}',
  400: '{slate.400}',
  500: '{slate.500}',
  600: '{slate.600}',
  700: '{slate.700}',
  800: '{slate.800}',
  900: '{slate.900}',
  950: '{slate.950}',
};

export const ComandixPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{cyan.50}',
      100: '{cyan.100}',
      200: '{cyan.200}',
      300: '{cyan.300}',
      400: '{cyan.400}',
      500: '{cyan.500}',
      600: '{cyan.600}',
      700: '{cyan.700}',
      800: '{cyan.800}',
      900: '{cyan.900}',
      950: '{cyan.950}',
    },
    colorScheme: {
      light: {
        primary: {
          color: '{cyan.700}',
          contrastColor: '#ffffff',
          hoverColor: '{cyan.800}',
          activeColor: '{cyan.900}',
        },
        surface: slate,
      },
      dark: {
        primary: {
          color: '{cyan.400}',
          contrastColor: '{slate.950}',
          hoverColor: '{cyan.300}',
          activeColor: '{cyan.200}',
        },
        surface: slate,
      },
    },
  },
});

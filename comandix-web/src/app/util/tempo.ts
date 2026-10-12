export function tempoDecorrido(desde: string, agora: number): string {
  const minutos = Math.floor((agora - Date.parse(desde)) / 60_000);

  if (minutos < 1) return 'agora';
  if (minutos < 60) return `há ${minutos} min`;

  return `há ${Math.floor(minutos / 60)} h ${minutos % 60} min`;
}
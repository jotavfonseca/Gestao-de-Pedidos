export const STATUS_SEQUENCE = ['Pendente', 'Processando', 'Finalizado']

export const STATUS_STYLES = {
  Pendente: { badge: 'bg-amber-50 text-amber-800 ring-amber-200', dot: 'bg-amber-500' },
  Processando: { badge: 'bg-sky-50 text-sky-800 ring-sky-200', dot: 'bg-sky-500' },
  Finalizado: { badge: 'bg-emerald-50 text-emerald-800 ring-emerald-200', dot: 'bg-emerald-500' },
}

export const STATUS_DESCRIPTIONS = {
  Pendente: 'Pedido registrado e aguardando processamento.',
  Processando: 'O worker recebeu a mensagem e está processando o pedido.',
  Finalizado: 'Processamento concluído.',
}

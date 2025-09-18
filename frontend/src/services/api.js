/* eslint-disable no-undef */
const BASE = 'http://localhost:5000';
/* eslint-enable no-undef */

export async function fetchOrders() {
  const res = await fetch(`${BASE}/orders`);
  if (!res.ok) throw new Error('Erro ao buscar pedidos');
  return res.json();
}

export async function fetchOrder(id) {
  const res = await fetch(`${BASE}/orders/${id}`);
  if (!res.ok) throw new Error('Erro ao buscar pedido');
  return res.json();
}

export async function createOrder(payload) {
  const res = await fetch(`${BASE}/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || 'Erro ao criar pedido');
  }
  return res.json();
}

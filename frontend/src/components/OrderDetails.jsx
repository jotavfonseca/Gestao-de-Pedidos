import React, { useState } from "react";
import { createOrder } from "../services/api";
import { useNavigate } from "react-router-dom";

export default function CreateOrder(){
  const [cliente, setCliente] = useState("");
  const [produto, setProduto] = useState("");
  const [valor, setValor] = useState("");
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState(null);
  const navigate = useNavigate();

  const submit = async (e) => {
    e.preventDefault();
    setErr(null);
    setLoading(true);
    try {
      const payload = {
        cliente,
        produto,
        valor: Number(valor)
      };
      const created = await createOrder(payload);
      navigate(`/orders/${created.id}`);
    } catch (e) {
      setErr(e.message || "Erro");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-lg bg-white p-6 rounded shadow">
      <h2 className="text-lg font-semibold mb-4">Novo Pedido</h2>
      {err && <p className="text-red-600 mb-2">{err}</p>}
      <form onSubmit={submit} className="space-y-4">
        <div>
          <label className="block text-sm font-medium">Cliente</label>
          <input value={cliente} onChange={e=>setCliente(e.target.value)} required className="mt-1 block w-full border rounded px-3 py-2"/>
        </div>
        <div>
          <label className="block text-sm font-medium">Produto</label>
          <input value={produto} onChange={e=>setProduto(e.target.value)} required className="mt-1 block w-full border rounded px-3 py-2"/>
        </div>
        <div>
          <label className="block text-sm font-medium">Valor (R$)</label>
          <input value={valor} onChange={e=>setValor(e.target.value)} type="number" step="0.01" required className="mt-1 block w-full border rounded px-3 py-2"/>
        </div>
        <div className="flex gap-2">
          <button disabled={loading} className="bg-sky-600 text-white px-4 py-2 rounded">{loading ? "Salvando..." : "Salvar"}</button>
          <button type="button" onClick={() => navigate(-1)} className="px-4 py-2 rounded border">Cancelar</button>
        </div>
      </form>
    </div>
  );
}

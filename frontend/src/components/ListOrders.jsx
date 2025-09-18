import React, { useEffect, useState } from "react";
import { fetchOrders } from "../services/api";
import { Link } from "react-router-dom";

function StatusBadge({ status }) {
  const map = {
    Pendente: "bg-yellow-100 text-yellow-800",
    Processando: "bg-blue-100 text-blue-800",
    Finalizado: "bg-green-100 text-green-800"
  };
  return <span className={`px-2 py-1 rounded text-sm font-medium ${map[status] || "bg-gray-100 text-gray-800"}`}>{status}</span>;
}

export default function ListOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState(null);

  const load = async () => {
    try {
      setLoading(true);
      const data = await fetchOrders();
      setOrders(data);
    } catch (e) {
      setErr(e.message || e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  return (
    <div>
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-lg font-semibold">Pedidos</h2>
        <Link to="/create" className="bg-sky-600 text-white px-3 py-1 rounded">Novo Pedido</Link>
      </div>

      {loading && <p>Carregando...</p>}
      {err && <p className="text-red-600">{err}</p>}

      {!loading && orders.length === 0 && <p>Nenhum pedido encontrado.</p>}

      {!loading && orders.length > 0 && (
        <div className="overflow-x-auto bg-white rounded shadow">
          <table className="min-w-full divide-y">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-medium">ID</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Cliente</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Produto</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Valor</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Status</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Criado em</th>
                <th className="px-4 py-3 text-left text-sm font-medium">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {orders.map(o => (
                <tr key={o.id} className="hover:bg-gray-50">
                  <td className="px-4 py-2 text-sm">{o.id.substring(0,8)}</td>
                  <td className="px-4 py-2 text-sm">{o.cliente}</td>
                  <td className="px-4 py-2 text-sm">{o.produto}</td>
                  <td className="px-4 py-2 text-sm">R$ {Number(o.valor).toFixed(2)}</td>
                  <td className="px-4 py-2 text-sm"><StatusBadge status={o.status} /></td>
                  <td className="px-4 py-2 text-sm">{new Date(o.dataCriacao).toLocaleString()}</td>
                  <td className="px-4 py-2 text-sm">
                    <Link to={`/orders/${o.id}`} className="text-sky-600 hover:underline">Detalhes</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

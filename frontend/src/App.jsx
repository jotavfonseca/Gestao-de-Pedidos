import React from "react";
import { Routes, Route, Link } from "react-router-dom";
import ListOrders from "./components/ListOrders";
import CreateOrder from "./components/CreateOrder";
import OrderDetails from "./components/OrderDetails";

export default function App() {
  return (
    <div className="min-h-screen">
      <header className="bg-white shadow-sm">
        <div className="container mx-auto px-4 py-4 flex justify-between items-center">
          <h1 className="text-xl font-semibold">Gestão de Pedidos</h1>
          <nav className="space-x-4">
            <Link to="/" className="text-sky-600 hover:underline">Pedidos</Link>
            <Link to="/create" className="text-sky-600 hover:underline">Novo Pedido</Link>
          </nav>
        </div>
      </header>

      <main className="container mx-auto px-4 py-6">
        <Routes>
          <Route path="/" element={<ListOrders/>} />
          <Route path="/create" element={<CreateOrder/>} />
          <Route path="/orders/:id" element={<OrderDetails/>} />
        </Routes>
      </main>
    </div>
  );
}

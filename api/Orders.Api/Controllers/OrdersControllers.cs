using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;
using System;
using System.Threading.Tasks;
using OrderApi.Services;

namespace OrderApi.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderContext _context;

        public OrdersController(OrderContext context)
        {
            _context = context;
        }

        // POST /orders
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Order order)
        {
            try
            {
                order.Id = Guid.NewGuid();
                order.Status = OrderStatus.Pendente;
                order.DataCriacao = DateTime.UtcNow;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Nenhuma publicação na fila

                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao criar o pedido", ex);
            }
        }



        // GET /orders
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders.ToListAsync();
            return Ok(orders);
        }

        // GET /orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}

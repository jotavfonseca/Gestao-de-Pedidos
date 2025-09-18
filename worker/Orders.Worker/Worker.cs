using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OrderApi.Data;
using OrderApi.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderWorker
{
    public class Worker : BackgroundService
    {
        private readonly OrderContext _context;

        public Worker(OrderContext context)
        {
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pendente)
                    .ToListAsync(stoppingToken);

                foreach (var order in pendingOrders)
                {
                    if (order.Status != OrderStatus.Pendente)
                        continue;

                    order.Status = OrderStatus.Processando;
                    _context.Historico.Add(new OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Status = OrderStatus.Processando,
                        Data = DateTimeOffset.UtcNow
                    });
                    await _context.SaveChangesAsync(stoppingToken);

                    await Task.Delay(5000, stoppingToken);

                    order.Status = OrderStatus.Finalizado;
                    _context.Historico.Add(new OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Status = OrderStatus.Finalizado,
                        Data = DateTimeOffset.UtcNow
                    });
                    await _context.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}

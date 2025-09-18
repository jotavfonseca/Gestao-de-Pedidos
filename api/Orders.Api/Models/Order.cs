using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderApi.Models
{
    public enum OrderStatus
    {
        Pendente = 0,
        Processando = 1,
        Finalizado = 2
    }

    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Cliente { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Produto { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pendente;

        [Required]
        public DateTimeOffset DataCriacao { get; set; } = DateTimeOffset.UtcNow;

        // Histórico de status
        public List<OrderStatusHistory> Historico { get; set; } = new();
    }

    public class OrderStatusHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [Required]
        public DateTimeOffset Data { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey("OrderId")]
        public Order Order { get; set; } = null!;
    }
}

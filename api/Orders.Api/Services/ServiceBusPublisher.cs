using Azure.Messaging.ServiceBus;
using OrderApi.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderApi.Services
{
    public class ServiceBusPublisher
    {
        private readonly ServiceBusClient _client;
        private readonly string _queueName;

        public ServiceBusPublisher(string connectionString, string queueName)
        {
            _client = new ServiceBusClient(connectionString);
            _queueName = queueName;
        }

        public async Task PublishOrderCreatedAsync(Order order)
        {
            var sender = _client.CreateSender(_queueName);

            // Cria mensagem com JSON
            var messageBody = JsonSerializer.Serialize(order);
            var message = new ServiceBusMessage(messageBody)
            {
                CorrelationId = order.Id.ToString(),
                Subject = "OrderCreated"
            };

            // Envia a mensagem
            await sender.SendMessageAsync(message);
        }
    }
}

using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace FinanceAPI.Application.Services
{
    public class RabbitMqPublisher
    {
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;
        }

        public async Task PublishTransactionEventAsync(object transactionData)
        {
            try
            {
                var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";
                var factory = new ConnectionFactory { HostName = hostName, UserName = "rondon_admin", Password = "dev_password123" };
                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: "finance_events", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var message = JsonSerializer.Serialize(transactionData);
                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "finance_events", body: body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao conectar ou publicar no RabbitMQ. O sistema continuará operando.");
            }
        }
    }
}

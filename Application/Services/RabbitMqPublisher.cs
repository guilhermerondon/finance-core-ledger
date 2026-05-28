using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace FinanceAPI.Application.Services
{
    public class RabbitMqPublisher
    {
        public void PublishTransactionEvent(object transactionData)
        {
            var factory = new ConnectionFactory { HostName = "localhost", UserName = "rondon_admin", Password = "dev_password123" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "finance_events", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var message = JsonSerializer.Serialize(transactionData);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: string.Empty, routingKey: "finance_events", basicProperties: null, body: body);
        }
    }
}

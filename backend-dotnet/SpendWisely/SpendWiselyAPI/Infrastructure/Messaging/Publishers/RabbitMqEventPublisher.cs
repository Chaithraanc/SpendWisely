using RabbitMQ.Client;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using System.Text;
using System.Text.Json;

namespace SpendWiselyAPI.Infrastructure.Messaging.Publishers
{
    public class RabbitMqEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private const string ExchangeName = "spendwisely.events";
       // private const string RoutingKey = "expense.changed";
       // private const string QueueName = "expense-events";

        public RabbitMqEventPublisher()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
              
            };
            // Create connection and channel once, reuse for all messages
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare exchange by publisher only, queues and bindings are the responsibility of the consumer
            _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false
            );
            // consumers can declare their own queues and bind to the exchange with appropriate routing keys, but the publisher should not be responsible for that.
            // This allows for better separation of concerns and flexibility in how consumers consume messages.

            //_channel.QueueDeclareAsync(
            //    queue: QueueName,
            //    durable: true,
            //    exclusive: false,
            //    autoDelete: false,
            //    arguments: null
            //);

            //_channel.QueueBindAsync(
            //    queue: QueueName,
            //    exchange: ExchangeName,
            //    routingKey: RoutingKey
            //);
        }

        public Task PublishEventAsync<T>(T Event, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var message = JsonSerializer.Serialize(Event);
            var body = Encoding.UTF8.GetBytes(message);

            var props = new BasicProperties
            {
                Persistent = true // Ensure messages are persisted to disk
            };

            // Fix: Use reflection to get EventType property if it exists, otherwise fallback to default
            string eventType = Event?.GetType().GetProperty("EventType")?.GetValue(Event)?.ToString() ?? "Unknown";

            // Determine routing key based on event type, this allows for more flexible routing and multiple consumers can bind to the same exchange with different routing keys if needed.
            var routingKey = eventType switch
            {
                "ExpenseCreated" => "expense.created",
                "ExpenseUpdated" => "expense.updated",
                "ExpenseDeleted" => "expense.deleted",
                "ExpenseCategorized" => "expense.categorized",
                "BudgetCreated" => "budget.created",
                "BudgetUpdated" => "budget.updated",
                "BudgetDeleted" => "budget.deleted",
                "BudgetExceeded" => "budget.exceeded",
                "MonthlySummaryGenerated" => "monthly.summary.generated",
                "AIInsightGenerated" => "ai.insight.generated",
                _ => "expense.unknown"
            };

            //publish to exchange
            _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: props,
                body: body
            );

            return Task.CompletedTask;
        }
        // Since RabbitMQ connections and channels are IDisposable, we should implement IDisposable to clean up resources properly.
        public void Dispose()
        {

            if (_channel != null && !_channel.IsClosed)
            {
                _channel.CloseAsync(200, "Disposing", false).GetAwaiter().GetResult();
            }
          
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}

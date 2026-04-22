using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SpendWiselyAPI.Application.Events;
using SpendWiselyAPI.Application.Interfaces;
using SpendWiselyAPI.Infrastructure.Events.Models;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SpendWiselyAPI.Infrastructure.Messaging.Consumers
{
 
        public class MongoDBConsumer : BackgroundService
        {
            private readonly IConnection _connection;
            private readonly IChannel _channel;
          //  private readonly IModel _channel;
            private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MongoDBConsumer> _logger;

        private const string MainQueue = "spendwisely.eventstore.main";
        private const string RetryQueue = "spendwisely.eventstore.retry";
        private const string DlqQueue = "spendwisely.eventstore.dlq";

        private const string MainExchange = "spendwisely.events";
        private const string RetryExchange = "spendwisely.eventstore.retry.exchange";
        private const string DlqExchange = "spendwisely.eventstore.dlq.exchange";



        private const int RetryDelayMs = 5000;
        private const int MaxRetries = 3;

            public MongoDBConsumer(IServiceScopeFactory scopeFactory, ILogger<MongoDBConsumer> logger)
            {
                _scopeFactory = scopeFactory;
                _logger = logger;

            var factory = new ConnectionFactory
                {
                    HostName = "localhost",
                    
                };
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
              
                ConfigureQueues();
            _logger.LogInformation("MongoDBConsumer initialized and connected to RabbitMQ");
        }

            private void ConfigureQueues()
            {
                // MAIN QUEUE
                _channel.QueueDeclareAsync(
                    queue: MainQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                    { "x-dead-letter-exchange", RetryExchange }
                    }
                ).GetAwaiter().GetResult();

            _channel.QueueBindAsync(MainQueue, MainExchange, "").GetAwaiter().GetResult(); ;

                // RETRY QUEUE
                _channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult(); ;

                _channel.QueueDeclareAsync(
                    queue: RetryQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<string, object?>
                    {
                    { "x-message-ttl", RetryDelayMs },
                    { "x-dead-letter-exchange", MainExchange }
                    }
                ).GetAwaiter().GetResult(); ;

                _channel.QueueBindAsync(RetryQueue, RetryExchange, "").GetAwaiter().GetResult(); ;

                // DLQ
                _channel.ExchangeDeclareAsync(DlqExchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult(); ;

                _channel.QueueDeclareAsync(
                    queue: DlqQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                ).GetAwaiter().GetResult(); ;

                _channel.QueueBindAsync(DlqQueue, DlqExchange, "").GetAwaiter().GetResult(); ;
            }

            protected override Task ExecuteAsync(CancellationToken stoppingToken)
            {
            _logger.LogInformation("MongoDBConsumer started consuming messages from RabbitMQ");
            var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    _logger.LogInformation($"Received message: {json}");
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                    var processedRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventsRepository>();
                    var mongoRepo = scope.ServiceProvider.GetRequiredService<IEventStoreRepository>();

                        var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        Guid aggregateId =
                        TryGetGuid(root, "AggregateId") ??
                        TryGetGuid(root, "ExpenseId") ??
                        TryGetGuid(root, "BudgetId") ??
                        TryGetGuid(root, "CategoryId") ??
                        TryGetGuid(root, "Id") ??
                        Guid.Empty;
                        // CREATE ENVELOPE - we create a domain event envelope to encapsulate the event details, this allows us to store the entire JSON payload in MongoDB while still having structured access to important metadata like event ID, type, aggregate ID, user ID and timestamp for processing and auditing purposes.
                        var envelope = new DomainEventEnvelope<object>
                        {
                            EventId = root.GetProperty("EventId").GetGuid(),
                            EventType = root.GetProperty("EventType").GetString(),
                            AggregateId = aggregateId,
                            UserId = root.GetProperty("UserId").GetGuid(),
                            Payload = root.GetRawText(),
                          //  Payload = JsonDocument.Parse(root.GetRawText()).RootElement.Clone(),

                            TimeStamp = root.GetProperty("Timestamp").GetDateTime()
                        };


                        if (envelope == null)
                    {
                        // Optionally log or handle the null envelope case
                        
                            _logger.LogWarning("Received message could not be parsed into a valid DomainEventEnvelope. Message will be acknowledged and discarded.");
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }
                   
                        // IDEMPOTENCY CHECK - ensures that we do not process the same event multiple times in case of retries or duplicates, we check if the event has already been processed by looking it up in the processed events table using the event ID ,type and entityID as a unique identifier.
                    if (await processedRepo.ExistsAsync(envelope.EventId , envelope.EventType))
                    {
                            await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            _logger.LogInformation($"Event with ID {envelope.EventId}  has already been processed. Acknowledging message and skipping processing.");
                            return;
                    }

                    // START TRANSACTION - ensures both event saving in mongoDB and processed marking in SQL are atomic
                    using var tx = await processedRepo.BeginTransactionAsync(stoppingToken);
                   _logger.LogInformation($"Processing event with ID {envelope.EventId}. Marking as processed and saving to MongoDB.");

                        // MARK AS PROCESSED - this will prevent reprocessing of the same event in case of retries or duplicates, we save the event details in the processed events table for auditing and debugging purposes.
                        await processedRepo.MarkProcessedAsync(
                        envelope.EventId,
                        envelope.AggregateId,
                        envelope.EventType,
                        envelope.UserId,
                        JsonSerializer.Serialize(envelope.Payload)
                    );
                        

                        // SAVE ALL CHANGES
                        await processedRepo.SaveChangesAsync(stoppingToken);
                        await tx.CommitAsync(stoppingToken);
                        _logger.LogInformation($"Event with ID {envelope.EventId} marked as processed in SQL database.");
                        // PROCESS EVENT
                        await mongoRepo.SaveEventAsync(envelope, stoppingToken);
                        _logger.LogInformation($"Event with ID {envelope.EventId} saved to MongoDB event store.");
                        // ACK MESSAGE - only after successful processing and marking as processed, we acknowledge the message to RabbitMQ, this ensures that if any step fails, the message will not be acknowledged and will be retried according to the retry policy.
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Mongo Consumer Error:"+  ex);
                        _logger.LogError(ex, "Error processing message. Message will be retried or sent to DLQ based on retry count.");
                        int retryCount = GetRetryCount(ea.BasicProperties);

                        if (retryCount >= MaxRetries)
                        {
                            // SEND TO DLQ
                            var properties = new BasicProperties();
                            properties.Headers = null; // For DLQ, no headers needed

                            await _channel.BasicPublishAsync(
                            exchange: DlqExchange,
                            routingKey: "",
                            mandatory: false,
                            basicProperties: properties,
                            body: body
                            );

                           await _channel.BasicAckAsync(ea.DeliveryTag, false);
                            return;
                        }

                        // SEND TO RETRY QUEUE
                        var retryProperties = new BasicProperties();
                        retryProperties.Headers = new Dictionary<string, object?>
                        {
                         { "x-retry-count", retryCount + 1 }
                        };

                        await _channel.BasicPublishAsync(
                        exchange: RetryExchange,
                        routingKey: "",
                        mandatory: false,
                        basicProperties: retryProperties,
                        body: body
                        );

                       await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                };

                _channel.BasicConsumeAsync(
                    queue: MainQueue,
                    autoAck: false,
                    consumer: consumer
                );
           
            return Task.CompletedTask;
            }

            private int GetRetryCount(IReadOnlyBasicProperties props)
            {
                if (props?.Headers != null &&
                    props.Headers.TryGetValue("x-retry-count", out var value))
                {
                    return Convert.ToInt32(value);
                }

                return 0;
            }

            public void Dispose()
            {

                if (_channel != null && !_channel.IsClosed)
                {
                    _channel.CloseAsync(200, "Disposing", false).GetAwaiter().GetResult();
                }

                _channel?.Dispose();
                _connection?.Dispose();
            }

        private static Guid? TryGetGuid(JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var prop) &&
                Guid.TryParse(prop.GetString(), out var value))
            {
                return value;
            }
            return null;
        }


    }
}

  

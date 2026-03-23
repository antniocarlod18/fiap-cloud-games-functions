using Elastic.Apm;
using Elastic.Apm.Api;
using FiapCloudGames.Contracts.IntegrationEvents;
using MassTransit;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fiap_cloud_games_functions;

public class FunctionApps
{
    private readonly ILogger _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public FunctionApps(ILoggerFactory loggerFactory, IPublishEndpoint publishEndpoint)
    {
        _logger = loggerFactory.CreateLogger<FunctionApps>();
        _publishEndpoint = publishEndpoint;
    }    

    [Function("FunctionProcessPayment")]
    public async Task ProcessPayment([RabbitMQTrigger("payment-queue", ConnectionStringSetting = "RabbitMQ:Connection")] string myQueueItem,
        IDictionary<string, object> headers)
    {
        headers.TryGetValue("traceparent", out var traceParentObj);
        var traceParent = traceParentObj?.ToString();

        ITransaction? transaction = null;

        if (!string.IsNullOrEmpty(traceParent))
        {
            var distributedTracingData = DistributedTracingData.TryDeserializeFromString(traceParent);

            transaction = Agent.Tracer.StartTransaction(
                "ProcessPayment",
                ApiConstants.TypeRequest,
                distributedTracingData
            );
        }
        else
        {
            // fallback (sem trace)
            transaction = Agent.Tracer.StartTransaction(
                "ProcessPayment",
                ApiConstants.TypeRequest
            );
        }

        _logger.LogInformation("Received payment message: {item}", myQueueItem);

        try
        {    
             var msg = JsonSerializer.Deserialize<PaymentMessage>(myQueueItem, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (msg == null)
            {
                _logger.LogWarning("Payment message deserialized to null");
                return;
            }

            // Simulate payment processing
            _logger.LogInformation("Processing payment for order {orderId} amount {amount} method {method}", msg.OrderId, msg.Price, msg.Method);
            await Task.Delay(200);

            var status = msg.Price > 0 && msg.Price <= 1000m ? PaymentStatusEnum.Approved : PaymentStatusEnum.Rejected;

            SendPaymentProcessed(msg.OrderId, msg.UserId, status);
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Failed to deserialize payment message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment message");
        }
    }

    [Function("FunctionSendNotification")]
    public async Task SendNotification([RabbitMQTrigger("notification-queue", ConnectionStringSetting = "RabbitMQ:Connection")] string myQueueItem,
        IDictionary<string, object> headers)
    {
        headers.TryGetValue("traceparent", out var traceParentObj);
        var traceParent = traceParentObj?.ToString();

        ITransaction? transaction = null;

        if (!string.IsNullOrEmpty(traceParent))
        {
            var distributedTracingData = DistributedTracingData.TryDeserializeFromString(traceParent);

            transaction = Agent.Tracer.StartTransaction(
                "SendNotification",
                ApiConstants.TypeRequest,
                distributedTracingData
            );
        }
        else
        {
            // fallback (sem trace)
            transaction = Agent.Tracer.StartTransaction(
                "SendNotification",
                ApiConstants.TypeRequest
            );
        }

        _logger.LogInformation("Received notification message: {item}", myQueueItem);

        try
        {
            var msg = JsonSerializer.Deserialize<NotificationMessage>(myQueueItem, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (msg == null)
            {
                _logger.LogWarning("Notification message deserialized to null");
                return;
            }

            await ProcessNotificationAsync(msg);
        }
        catch (JsonException jex)
        {
            _logger.LogError(jex, "Failed to deserialize notification message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification message");
        }
    }

    private async Task ProcessNotificationAsync(NotificationMessage msg)
    {
        // Simulate sending notification (e.g., push, email, sms)
        _logger.LogInformation("Sending notification to {email}: {title} - {message}", msg.Email, msg.Title, msg.Message);

        // Simulate async I/O or external call
        await Task.Delay(100);

        _logger.LogInformation("Notification sent to {email}", msg.Email);
    }

    public async Task SendPaymentProcessed(string orderId, string userId, PaymentStatusEnum status)
    {
        try
        {
            await _publishEndpoint.Publish<PaymentProcessedIntegrationEvent>(new()
            {
                OrderId = Guid.Parse(orderId),
                UserId = Guid.Parse(userId),
                Status = status
            });

            _logger.LogInformation("Published PaymentProcessedIntegrationEvent for OrderId: {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing PaymentProcessedIntegrationEvent for OrderId: {OrderId}", orderId);
            throw;
        }

        return;
    }

    private class NotificationMessage
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    private class PaymentMessage
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "BRL";

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;
    }
}
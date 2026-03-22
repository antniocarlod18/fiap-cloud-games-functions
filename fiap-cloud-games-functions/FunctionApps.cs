using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fiap_cloud_games_functions;

public class FunctionApps
{
    private readonly ILogger _logger;

    public FunctionApps(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FunctionApps>();
    }

    [Function("FunctionSendNotification")]
    public async Task SendNotification([RabbitMQTrigger("queue:notification-queue", ConnectionStringSetting = "RabbitMQ:Connection")] string myQueueItem)
    {
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

    private class NotificationMessage
    {
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
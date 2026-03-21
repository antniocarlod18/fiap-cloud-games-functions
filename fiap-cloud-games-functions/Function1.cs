using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace fiap_cloud_games_functions;

public class Function1
{
    private readonly ILogger _logger;

    public Function1(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Function1>();
    }

    [Function("Function1")]
    public void Run([RabbitMQTrigger("myqueue", ConnectionStringSetting = "QueueConnection")] string myQueueItem)
    {
        _logger.LogInformation("C# Queue trigger function processed: {item}", myQueueItem);
    }
}
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Contracts;

namespace SEOPackagingAgent;

public sealed class AgentWorker<TAgent>(TAgent agent, ILogger<AgentWorker<TAgent>> logger) : BackgroundService where TAgent : AgentBase
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{AgentName} listening for messages", typeof(TAgent).Name);
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

public sealed class SeoPackagingAgentHandler(
    Azure.Messaging.ServiceBus.ServiceBusClient busClient,
    Dapr.Client.DaprClient daprClient,
    Azure.AI.OpenAI.OpenAIClient openAiClient,
    ILoggerFactory loggerFactory,
    Microsoft.Extensions.Options.IOptions<AgentOptions> options) : AgentBase(busClient, daprClient, openAiClient, loggerFactory, options)
{
    protected override async Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct)
    {
        if (env.Payload is CopyEditResult copy)
        {
            _logger.LogInformation("Packaging SEO for {StoryId}", copy.StoryId.Value);
            var pkg = new PackagingResult(copy.StoryId, copy.HeadlineVariants.First(), "SEO description", new { }, "https://cdn.example/image.jpg");
            await PublishAsync("package", pkg, ct);
        }
    }
}

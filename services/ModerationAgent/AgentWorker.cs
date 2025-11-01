using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Contracts;

namespace ModerationAgent;

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

public sealed class ModerationAgentHandler(
    Azure.Messaging.ServiceBus.ServiceBusClient busClient,
    Dapr.Client.DaprClient daprClient,
    Azure.AI.OpenAI.OpenAIClient openAiClient,
    ILoggerFactory loggerFactory,
    Microsoft.Extensions.Options.IOptions<AgentOptions> options) : AgentBase(busClient, daprClient, openAiClient, loggerFactory, options)
{
    protected override Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct)
    {
        if (env.Payload is DistributionPlan plan)
        {
            _logger.LogInformation("Moderating distribution plan for {StoryId} across {Channels}", plan.StoryId.Value, string.Join(",", plan.Channels));
        }

        return Task.CompletedTask;
    }
}

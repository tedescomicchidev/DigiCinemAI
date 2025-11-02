using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Contracts;
using Newsroom.Cms;

namespace ReporterAgent;

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

public sealed class ReporterAgentHandler(
    Azure.Messaging.ServiceBus.ServiceBusClient busClient,
    Dapr.Client.DaprClient daprClient,
    Azure.AI.OpenAI.OpenAIClient openAiClient,
    ILoggerFactory loggerFactory,
    Microsoft.Extensions.Options.IOptions<AgentOptions> options,
    RecirculationService recirculation) : AgentBase(busClient, daprClient, openAiClient, loggerFactory, options)
{
    protected override async Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct)
    {
        if (env.Payload is Assignment assignment)
        {
            _logger.LogInformation("Drafting story for {Desk}", assignment.Desk);
            var draft = new Draft(
                assignment.StoryId,
                $"Auto hed for {assignment.Desk}",
                "Auto dek",
                "Generated body...",
                new[] { "automation" },
                Array.Empty<string>(),
                false);
            await PublishAsync("drafts", draft, ct);
        }
    }
}

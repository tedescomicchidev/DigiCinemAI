using FluentValidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Contracts;
using Newsroom.Contracts.Validation;

namespace AssignmentAgent;

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

public sealed class AssignmentAgentHandler(
    Azure.Messaging.ServiceBus.ServiceBusClient busClient,
    Dapr.Client.DaprClient daprClient,
    Azure.AI.OpenAI.OpenAIClient openAiClient,
    ILoggerFactory loggerFactory,
    Microsoft.Extensions.Options.IOptions<AgentOptions> options,
    StoryPitchValidator validator) : AgentBase(busClient, daprClient, openAiClient, loggerFactory, options)
{
    protected override async Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct)
    {
        if (env.Payload is StoryPitch pitch)
        {
            validator.ValidateAndThrow(pitch);
            var assignment = new Assignment(pitch.StoryId, "Digital Desk", "AutoPlanner", DateTimeOffset.UtcNow.AddHours(6), Array.Empty<string>());
            _logger.LogInformation("Assigned story {StoryId} to {Assignee}", assignment.StoryId.Value, assignment.Assignee);
            await PublishAsync("assignments", assignment, ct);
        }
    }
}

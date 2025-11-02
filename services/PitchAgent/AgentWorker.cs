using FluentValidation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Contracts;
using Newsroom.Contracts.Validation;

namespace PitchAgent;

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

public sealed class PitchAgentHandler(
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
            _logger.LogInformation("Publishing pitch {StoryId}", pitch.StoryId.Value);
            await PublishAsync("pitches", pitch, ct);
        }
        else
        {
            _logger.LogInformation("Received envelope {EnvelopeId}", env.Id);
        }
    }
}

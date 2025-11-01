using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newsroom.Agents;
using Newsroom.Cms;
using Newsroom.Contracts;

namespace PublishAgent;

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

public sealed class PublishAgentHandler(
    Azure.Messaging.ServiceBus.ServiceBusClient busClient,
    Dapr.Client.DaprClient daprClient,
    Azure.AI.OpenAI.OpenAIClient openAiClient,
    ILoggerFactory loggerFactory,
    Microsoft.Extensions.Options.IOptions<AgentOptions> options,
    ICmsPublisher cmsPublisher,
    SyndicationService syndicationService) : AgentBase(busClient, daprClient, openAiClient, loggerFactory, options)
{
    protected override async Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct)
    {
        if (env.Payload is PackagingResult package)
        {
            _logger.LogInformation("Publishing story {StoryId}", package.StoryId.Value);
            var cmsId = await cmsPublisher.CreateOrUpdateAsync(
                new Draft(package.StoryId, package.SeoTitle, package.SeoDescription, "Body", Array.Empty<string>(), Array.Empty<string>(), false),
                package,
                ct);
            await cmsPublisher.ScheduleAsync(cmsId, DateTimeOffset.UtcNow.AddMinutes(15), ct);
            await PublishAsync("publish", new PublishRequest(package.StoryId, DateTimeOffset.UtcNow.AddMinutes(15)), ct);
            await syndicationService.GenerateRssAsync(new PublishRequest(package.StoryId, null), ct);
        }
    }
}

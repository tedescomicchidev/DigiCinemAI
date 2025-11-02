using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Azure.Messaging.ServiceBus;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newsroom.Contracts;
using Polly;

namespace Newsroom.Agents;

public abstract class AgentBase
{
    private readonly ServiceBusClient _busClient;
    private readonly DaprClient _daprClient;
    private readonly OpenAIClient _openAiClient;
    protected readonly ILogger _logger;
    private readonly AsyncPolicy _resiliencyPolicy;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected AgentBase(
        ServiceBusClient busClient,
        DaprClient daprClient,
        OpenAIClient openAiClient,
        ILoggerFactory loggerFactory,
        IOptions<AgentOptions> options)
    {
        _busClient = busClient;
        _daprClient = daprClient;
        _openAiClient = openAiClient;
        _logger = loggerFactory.CreateLogger(GetType());
        _resiliencyPolicy = Policy
            .Handle<RequestFailedException>()
            .Or<ServiceBusException>()
            .Or<OpenAIException>()
            .WaitAndRetryAsync(options.Value.RetryCount, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));
    }

    public async Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["StoryId"] = (envelope.Payload as StoryPitch)?.StoryId.Value.ToString() ?? string.Empty
        });

        await _resiliencyPolicy.ExecuteAsync(async _ => await HandleInternalAsync(envelope, cancellationToken), cancellationToken);
    }

    protected abstract Task HandleInternalAsync(MessageEnvelope env, CancellationToken ct);

    protected async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        var envelope = new MessageEnvelope(
            Guid.NewGuid(),
            payload.GetType().Name,
            GetType().Name,
            Guid.NewGuid().ToString(),
            5,
            DateTimeOffset.UtcNow,
            payload);

        var body = JsonSerializer.Serialize(envelope, _serializerOptions);
        await using var sender = _busClient.CreateSender(topic);
        await sender.SendMessageAsync(new ServiceBusMessage(body), cancellationToken);
    }

    protected async Task<ChatCompletions> GetChatCompletionAsync(string deployment, string prompt, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage> { new(ChatRole.System, prompt) };
        return await _resiliencyPolicy.ExecuteAsync(async _ => await _openAiClient.GetChatCompletionsAsync(deployment, messages, cancellationToken: cancellationToken), cancellationToken);
    }
}

public sealed class AgentOptions
{
    public int RetryCount { get; set; } = 3;
    public string ContentSafetyPolicy { get; set; } = "standard";
}

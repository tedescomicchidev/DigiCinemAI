using Azure;
using Azure.AI.OpenAI;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newsroom.Agents;
using VisualsAgent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection("Agent"));

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connection = configuration["SERVICEBUS__CONNECTION"] ?? "Endpoint=sb://localhost/";
    return new ServiceBusClient(connection);
});

builder.Services.AddSingleton(new Dapr.Client.DaprClientBuilder().Build());

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var endpoint = new Uri(configuration["AZURE_OPENAI__ENDPOINT"] ?? "https://localhost");
    var key = configuration["AZURE_OPENAI__KEY"] ?? "local";
    return new OpenAIClient(endpoint, new AzureKeyCredential(key));
});

builder.Services.AddSingleton<VisualsAgentHandler>();
builder.Services.AddHostedService<AgentWorker<VisualsAgentHandler>>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/healthz");
app.MapGet("/", () => Results.Ok(new { service = "VisualsAgent", status = "ready" }));

await app.RunAsync();

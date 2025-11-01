using Microsoft.Extensions.Logging;
using Newsroom.Contracts;

namespace Newsroom.Cms;

public sealed class WordPressVipPublisher(ILogger<WordPressVipPublisher> logger) : ICmsPublisher
{
    public Task<string> CreateOrUpdateAsync(Draft draft, PackagingResult pkg, CancellationToken ct)
    {
        logger.LogInformation("Simulating WordPress VIP draft update for {Story}", draft.StoryId.Value);
        return Task.FromResult($"wpvip-{draft.StoryId.Value}");
    }

    public Task ScheduleAsync(string cmsId, DateTimeOffset when, CancellationToken ct)
    {
        logger.LogInformation("Scheduling WordPress VIP post {CmsId} at {When}", cmsId, when);
        return Task.CompletedTask;
    }

    public Task PublishNowAsync(string cmsId, CancellationToken ct)
    {
        logger.LogInformation("Publishing WordPress VIP post {CmsId}", cmsId);
        return Task.CompletedTask;
    }
}

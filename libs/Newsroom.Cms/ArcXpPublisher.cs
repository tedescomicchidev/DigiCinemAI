using Microsoft.Extensions.Logging;
using Newsroom.Contracts;

namespace Newsroom.Cms;

public sealed class ArcXpPublisher(ILogger<ArcXpPublisher> logger) : ICmsPublisher
{
    public Task<string> CreateOrUpdateAsync(Draft draft, PackagingResult pkg, CancellationToken ct)
    {
        logger.LogInformation("Simulating Arc XP GraphQL mutation for {Story}", draft.StoryId.Value);
        return Task.FromResult($"arc-{draft.StoryId.Value}");
    }

    public Task ScheduleAsync(string cmsId, DateTimeOffset when, CancellationToken ct)
    {
        logger.LogInformation("Scheduling Arc XP post {CmsId} at {When}", cmsId, when);
        return Task.CompletedTask;
    }

    public Task PublishNowAsync(string cmsId, CancellationToken ct)
    {
        logger.LogInformation("Publishing Arc XP post {CmsId}", cmsId);
        return Task.CompletedTask;
    }
}

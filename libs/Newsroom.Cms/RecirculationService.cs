using Microsoft.Extensions.Logging;
using Newsroom.Contracts;

namespace Newsroom.Cms;

public sealed class RecirculationService(ILogger<RecirculationService> logger)
{
    public Task<IReadOnlyList<StoryId>> GetRelatedStoriesAsync(StoryId storyId, string[] tags, CancellationToken ct)
    {
        logger.LogInformation("Calculating recirculation for {StoryId} with {TagCount} tags", storyId.Value, tags.Length);
        return Task.FromResult<IReadOnlyList<StoryId>>(Array.Empty<StoryId>());
    }
}

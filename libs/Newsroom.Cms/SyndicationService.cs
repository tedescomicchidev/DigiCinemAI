using Microsoft.Extensions.Logging;
using Newsroom.Contracts;

namespace Newsroom.Cms;

public sealed class SyndicationService(ILogger<SyndicationService> logger)
{
    public Task<string> GenerateRssAsync(PublishRequest publishRequest, CancellationToken ct)
    {
        logger.LogInformation("Generating RSS entry for {StoryId}", publishRequest.StoryId.Value);
        return Task.FromResult("<rss><!-- stub --></rss>");
    }
}

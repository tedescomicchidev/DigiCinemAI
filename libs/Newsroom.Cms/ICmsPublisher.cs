namespace Newsroom.Cms;

using Newsroom.Contracts;

public interface ICmsPublisher
{
    Task<string> CreateOrUpdateAsync(Draft draft, PackagingResult pkg, CancellationToken ct);
    Task ScheduleAsync(string cmsId, DateTimeOffset when, CancellationToken ct);
    Task PublishNowAsync(string cmsId, CancellationToken ct);
}

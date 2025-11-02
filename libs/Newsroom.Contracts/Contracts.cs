namespace Newsroom.Contracts;

public enum StoryStage
{
    Pitched,
    Assigned,
    Reporting,
    Drafting,
    FactChecking,
    CopyEdit,
    Packaging,
    ReadyToPublish,
    Scheduled,
    Published,
    Distributed,
    Archived,
    Rework
}

public record StoryId(Guid Value);
public record AssetId(Guid Value);

public record StoryPitch(
    StoryId StoryId,
    string Slug,
    string HeadlineIdea,
    string Angle,
    string Beat,
    string[] Keywords,
    DateTimeOffset? EmbargoUntil,
    string[] Sources,
    string? Rationale,
    int Priority);

public record Assignment(
    StoryId StoryId,
    string Desk,
    string Assignee,
    DateTimeOffset Due,
    string[] RequiredAssets);

public record Draft(
    StoryId StoryId,
    string Hed,
    string Dek,
    string BodyMarkdown,
    string[] Tags,
    string[] Links,
    bool RequiresLegalReview);

public record FactCheckRequest(StoryId StoryId, string[] Claims, string DraftText);
public record FactCheckResult(StoryId StoryId, bool Pass, string[] Flags, string ReportMarkdown);
public record CopyEditRequest(StoryId StoryId, string Text);
public record CopyEditResult(StoryId StoryId, string Text, string[] HeadlineVariants);
public record PackagingRequest(StoryId StoryId, string Text, string[] Keywords);
public record PackagingResult(StoryId StoryId, string SeoTitle, string SeoDescription, object SchemaOrgJsonLd, string FeaturedImageUrl);
public record PublishRequest(StoryId StoryId, DateTimeOffset? ScheduleAt);
public record DistributionPlan(StoryId StoryId, string[] Channels, string[][] Posts);

public record MessageEnvelope(
    Guid Id,
    string Type,
    string Role,
    string CorrelationId,
    int Priority,
    DateTimeOffset CreatedAt,
    object Payload);

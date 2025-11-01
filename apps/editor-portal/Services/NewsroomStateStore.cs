using System.Collections.ObjectModel;
using System.Linq;
using Newsroom.Contracts;

namespace EditorPortal.Services;

public sealed class NewsroomStateStore
{
    public ObservableCollection<StorySummary> ActiveStories { get; } = new([
        new StorySummary(Guid.NewGuid(), "City Council approves new zoning rules", StoryStage.Drafting, 1),
        new StorySummary(Guid.NewGuid(), "Heatwave forecast triggers emergency planning", StoryStage.FactChecking, 2)
    ]);

    public ObservableCollection<OrchestrationState> LiveOrchestrations { get; } = new([
        new OrchestrationState(Guid.NewGuid(), "Drafting", 45),
        new OrchestrationState(Guid.NewGuid(), "FactChecking", 80)
    ]);

    public ObservableCollection<EscalationItem> Escalations { get; } = new([
        new EscalationItem(Guid.NewGuid(), "Investigative package", "Legal review required"),
        new EscalationItem(Guid.NewGuid(), "Sensitive interview", "Source verification pending")
    ]);

    public ObservableCollection<AnalyticsMetric> Analytics { get; } = new([
        new AnalyticsMetric("Subscribers Online", "32k", "+8% vs yesterday"),
        new AnalyticsMetric("Homepage CTR", "3.2%", "stable"),
        new AnalyticsMetric("Newsletter Signups", "640", "+12% week")
    ]);

    public CoveragePriorities Priorities { get; } = new("Politics", "Election integrity", "Metered");
    public PitchDraft NewPitch { get; private set; } = new(string.Empty, string.Empty, string.Empty, 3);

    public void ResolveEscalation(Guid id, bool approved)
    {
        var item = Escalations.FirstOrDefault(x => x.Id == id);
        if (item is not null)
        {
            Escalations.Remove(item);
        }
    }

    public Task SavePriorities()
    {
        // Persist to configuration or call orchestrator - stub for demo
        return Task.CompletedTask;
    }

    public Task SubmitPitchAsync()
    {
        ActiveStories.Add(new StorySummary(Guid.NewGuid(), NewPitch.Headline, StoryStage.Pitched, NewPitch.Priority));
        NewPitch = new PitchDraft(string.Empty, string.Empty, string.Empty, 3);
        return Task.CompletedTask;
    }
}

public sealed record StorySummary(Guid Id, string Headline, StoryStage Stage, int Priority);
public sealed record OrchestrationState(Guid StoryId, string State, int Progress);
public sealed record EscalationItem(Guid Id, string Title, string Reason);
public sealed record AnalyticsMetric(string Label, string Value, string Trend);
public sealed record CoveragePriorities(string PrimaryBeat, string SeoTheme, string PaywallMode);
public sealed class PitchDraft
{
    public PitchDraft(string headline, string beat, string angle, int priority)
    {
        Headline = headline;
        Beat = beat;
        Angle = angle;
        Priority = priority;
    }

    public string Headline { get; set; }
    public string Beat { get; set; }
    public string Angle { get; set; }
    public int Priority { get; set; }
}

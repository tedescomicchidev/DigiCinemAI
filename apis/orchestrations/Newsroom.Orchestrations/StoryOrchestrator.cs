using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newsroom.Contracts;

namespace Newsroom.Orchestrations;

public sealed class StoryOrchestrator
{
    [Function("StoryOrchestrator")]
    public static async Task Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var pitch = context.GetInput<StoryPitch>() ?? throw new InvalidOperationException("Pitch required");
        var assignment = await context.CallActivityAsync<Assignment>(nameof(AssignActivity), pitch);
        var draft = await context.CallActivityAsync<Draft>(nameof(ReporterActivity), assignment);
        var factCheck = await context.CallActivityAsync<FactCheckResult>(nameof(FactCheckActivity), draft);
        if (!factCheck.Pass)
        {
            await context.WaitForExternalEvent("EditorApproval");
        }
        var copy = await context.CallActivityAsync<CopyEditResult>(nameof(CopyEditActivity), draft);
        var package = await context.CallActivityAsync<PackagingResult>(nameof(PackagingActivity), copy);
        await context.CallActivityAsync(nameof(PublishActivity), package);
        await context.CallActivityAsync(nameof(DistributionActivity), package.StoryId);
    }

    [Function(nameof(AssignActivity))]
    public static Assignment AssignActivity([ActivityTrigger] StoryPitch pitch, FunctionContext context)
    {
        var logger = context.GetLogger("AssignActivity");
        logger.LogInformation("Assigning story {StoryId}", pitch.StoryId.Value);
        return new Assignment(pitch.StoryId, "Digital Desk", "WorkflowBot", DateTimeOffset.UtcNow.AddHours(4), Array.Empty<string>());
    }

    [Function(nameof(ReporterActivity))]
    public static Draft ReporterActivity([ActivityTrigger] Assignment assignment, FunctionContext context)
    {
        var logger = context.GetLogger("ReporterActivity");
        logger.LogInformation("Drafting story {StoryId}", assignment.StoryId.Value);
        return new Draft(assignment.StoryId, "Generated hed", "Generated dek", "Story body", Array.Empty<string>(), Array.Empty<string>(), false);
    }

    [Function(nameof(FactCheckActivity))]
    public static FactCheckResult FactCheckActivity([ActivityTrigger] Draft draft, FunctionContext context)
    {
        var logger = context.GetLogger("FactCheckActivity");
        logger.LogInformation("Fact checking {StoryId}", draft.StoryId.Value);
        return new FactCheckResult(draft.StoryId, true, Array.Empty<string>(), "Fact check passed");
    }

    [Function(nameof(CopyEditActivity))]
    public static CopyEditResult CopyEditActivity([ActivityTrigger] Draft draft, FunctionContext context)
    {
        var logger = context.GetLogger("CopyEditActivity");
        logger.LogInformation("Copy editing {StoryId}", draft.StoryId.Value);
        return new CopyEditResult(draft.StoryId, draft.BodyMarkdown, new[] { draft.Hed, $"Alt {draft.Hed}" });
    }

    [Function(nameof(PackagingActivity))]
    public static PackagingResult PackagingActivity([ActivityTrigger] CopyEditResult copy, FunctionContext context)
    {
        var logger = context.GetLogger("PackagingActivity");
        logger.LogInformation("Packaging SEO {StoryId}", copy.StoryId.Value);
        return new PackagingResult(copy.StoryId, copy.HeadlineVariants.First(), "SEO description", new { }, "https://cdn.example/img.jpg");
    }

    [Function(nameof(PublishActivity))]
    public static Task PublishActivity([ActivityTrigger] PackagingResult package, FunctionContext context)
    {
        var logger = context.GetLogger("PublishActivity");
        logger.LogInformation("Publishing {StoryId}", package.StoryId.Value);
        return Task.CompletedTask;
    }

    [Function(nameof(DistributionActivity))]
    public static Task DistributionActivity([ActivityTrigger] StoryId storyId, FunctionContext context)
    {
        var logger = context.GetLogger("DistributionActivity");
        logger.LogInformation("Distributing {StoryId}", storyId.Value);
        return Task.CompletedTask;
    }
}

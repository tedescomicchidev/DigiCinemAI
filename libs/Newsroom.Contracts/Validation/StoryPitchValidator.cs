using FluentValidation;

namespace Newsroom.Contracts.Validation;

public sealed class StoryPitchValidator : AbstractValidator<StoryPitch>
{
    public StoryPitchValidator()
    {
        RuleFor(x => x.StoryId).NotNull();
        RuleFor(x => x.StoryId.Value).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.HeadlineIdea).NotEmpty();
        RuleFor(x => x.Angle).NotEmpty();
        RuleFor(x => x.Beat).NotEmpty();
        RuleFor(x => x.Keywords).NotEmpty();
        RuleForEach(x => x.Keywords).NotEmpty();
        RuleFor(x => x.Priority).InclusiveBetween(0, 5);
    }
}

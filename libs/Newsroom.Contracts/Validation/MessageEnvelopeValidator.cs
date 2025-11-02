using FluentValidation;

namespace Newsroom.Contracts.Validation;

public sealed class MessageEnvelopeValidator : AbstractValidator<MessageEnvelope>
{
    public MessageEnvelopeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.CorrelationId).NotEmpty();
        RuleFor(x => x.CreatedAt).NotEmpty();
        RuleFor(x => x.Priority).InclusiveBetween(0, 10);
        RuleFor(x => x.Payload).NotNull();
    }
}

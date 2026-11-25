using Cbo.Core.Validation;
using FluentValidation;

namespace Cbo.Core.Friends;

public sealed class AddFriendRequestValidator : RequestValidator<AddFriendRequest>
{
    public AddFriendRequestValidator()
    {
        RuleFor(r => r.Username).NotEmpty().WithMessage("Username is required.");
    }

    public override string InvalidMessage => FriendFailures.NotAddedMessage;
}

using System.Linq.Expressions;
using Cbo.Core.Contracts.Topics;
using Cbo.Core.Models.Domain;

namespace Cbo.Core.Mappings;

/// <inheritdoc cref="TournamentProjections"/>
public static class TopicAuthorProjections
{
    public static Expression<Func<TopicAuthor, TopicAuthorResponse>> ToResponse() =>
        ta => new TopicAuthorResponse
        {
            Id = ta.Id,
            Username = ta.ApplicationUser!.UserName ?? string.Empty,
            IsOwner = ta.IsOwner,
            IsAuthor = ta.IsAuthor,
            TopicId = ta.TopicId,
            ApplicationUserId = ta.ApplicationUserId,
        };
}

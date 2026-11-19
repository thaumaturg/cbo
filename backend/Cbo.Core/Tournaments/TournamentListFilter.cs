using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Microsoft.EntityFrameworkCore;

namespace Cbo.Core.Tournaments;

/// <summary>The filters and sort keys of the tournaments list (ADR-0008). Every filter must translate to SQL.</summary>
public static class TournamentListFilter
{
    public static readonly SortKeys<Tournament> SortKeys = new SortKeys<Tournament>("-createdAt", t => t.Id)
        .Add("title", t => t.Title)
        // current_stage is stored as a string, and alphabetical order matches stage order up to Stage9
        .Add("stage", t => t.CurrentStage)
        .Add("createdAt", t => t.CreatedAt);

    /// <summary>Limits the tournaments to those the current user takes part in, then applies the query's filters.</summary>
    public static IQueryable<Tournament> Apply(IQueryable<Tournament> tournaments, GetTournamentsQuery query, Guid currentUserId)
    {
        ArgumentNullException.ThrowIfNull(tournaments);
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Tournament> filtered;
        if (query.Roles.Count == 0)
        {
            filtered = tournaments.Where(t => t.TournamentParticipants.Any(tp => tp.ApplicationUserId == currentUserId));
        }
        else
        {
            TournamentParticipantRole[] roles = [.. query.Roles];
            filtered = tournaments.Where(t => t.TournamentParticipants
                .Any(tp => tp.ApplicationUserId == currentUserId && roles.Contains(tp.Role)));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = LikePattern.Contains(query.Search);
            filtered = filtered.Where(t => EF.Functions.ILike(t.Title, pattern, LikePattern.EscapeCharacter));
        }

        filtered = query.Stage switch
        {
            TournamentStageFilter.Preparations => filtered.Where(t => t.CurrentStage == TournamentStage.Preparations),
            TournamentStageFilter.Started => filtered.Where(t => t.CurrentStage != TournamentStage.Preparations),
            _ => filtered,
        };

        return filtered;
    }
}

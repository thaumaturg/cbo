using Cbo.Core.Domain;
using Cbo.Core.Paging;
using Cbo.Core.Tournaments;
using Cbo.Core.Validation;
using Cbo.Tests.Paging;
using FluentValidation.TestHelper;

namespace Cbo.Tests.Tournaments;

public class GetTournamentsRequestValidatorTests : PagingRequestRulesTests<GetTournamentsRequest>
{
    protected override RequestValidator<GetTournamentsRequest> Validator { get; } = new GetTournamentsRequestValidator();

    protected override string KnownSortKey => "stage";

    protected override GetTournamentsRequest Request(int? page = null, int? pageSize = null, string? sort = null, string? search = null) =>
        new() { Page = page, PageSize = pageSize, Sort = sort, Search = search };

    protected override string? SearchOf(GetTournamentsRequest request) => request.Search;

    [Fact]
    public void An_unknown_sort_is_a_sort_error_listing_the_tournament_sort_keys()
    {
        Validator.TestValidate(Request(sort: "questionCount"))
            .ShouldHaveValidationErrorFor("sort")
            .WithErrorMessage("Unknown sort key. Allowed: title, stage, createdAt.")
            .Only();
    }

    [Theory]
    [InlineData(TournamentStage.Preparations)]
    [InlineData(TournamentStage.Qualifications)]
    [InlineData(TournamentStage.Playoff)]
    [InlineData(TournamentStage.Ended)]
    public void Every_stage_is_a_valid_filter(TournamentStage stage)
    {
        Validator.TestValidate(new GetTournamentsRequest { Stage = stage }).ShouldNotHaveAnyValidationErrors();
    }

    // Binding accepts numbers for enums, so an undefined value can reach the validator
    [Fact]
    public void An_undefined_stage_is_unknown_and_lists_every_stage()
    {
        Validator.TestValidate(new GetTournamentsRequest { Stage = (TournamentStage)4 })
            .ShouldHaveValidationErrorFor("stage")
            .WithErrorMessage("Unknown stage. Allowed: Preparations, Qualifications, Playoff, Ended.")
            .Only();
    }

    [Fact]
    public void Defined_roles_are_valid()
    {
        var request = new GetTournamentsRequest
        {
            Role = [TournamentParticipantRole.Player, TournamentParticipantRole.Organizer, TournamentParticipantRole.Creator],
        };

        Validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void An_empty_role_list_is_valid()
    {
        Validator.TestValidate(new GetTournamentsRequest { Role = [] }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void One_undefined_role_among_defined_ones_is_one_role_error()
    {
        var request = new GetTournamentsRequest
        {
            Role = [TournamentParticipantRole.Player, (TournamentParticipantRole)99, (TournamentParticipantRole)(-1)],
        };

        Validator.TestValidate(request)
            .ShouldHaveValidationErrorFor("role")
            .WithErrorMessage("Unknown role. Allowed: Player, Organizer, Creator.")
            .Only();
    }

    [Fact]
    public void Paging_and_filter_errors_are_reported_together()
    {
        TestValidationResult<GetTournamentsRequest> result = Validator.TestValidate(new GetTournamentsRequest
        {
            Page = 0,
            Search = new string('a', PagingDefaults.MaxSearchLength + 1),
            Stage = (TournamentStage)4,
            Role = [(TournamentParticipantRole)99],
        });

        Assert.Equal(["page", "search", "stage", "role"], result.Errors.ConvertAll(e => e.PropertyName));
    }
}

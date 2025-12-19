using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.API.Authorization;

public static class TopicOperations
{
    public static readonly OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Update =
        new() { Name = nameof(Update) };
    public static readonly OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };
    public static readonly OperationAuthorizationRequirement ManageAuthors =
        new() { Name = nameof(ManageAuthors) };
}

public static class TournamentOperations
{
    public static readonly OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Update =
        new() { Name = nameof(Update) };
    public static readonly OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };
    public static readonly OperationAuthorizationRequirement ManageParticipants =
        new() { Name = nameof(ManageParticipants) };
    public static readonly OperationAuthorizationRequirement ViewAllTopics =
        new() { Name = nameof(ViewAllTopics) };
    public static readonly OperationAuthorizationRequirement AdvanceStage =
        new() { Name = nameof(AdvanceStage) };
}

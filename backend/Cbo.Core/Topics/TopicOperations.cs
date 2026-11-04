using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Cbo.Core.Topics;

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

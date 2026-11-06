using Cbo.Api.ErrorHandling;
using Cbo.Core.Accounts;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder auth = api.MapGroup("Auth").WithTags("Auth");

        auth.MapPost("Register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous()
            .Produces<RegisterResponse>()
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict);

        auth.MapPost("Login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("ChangePassword", ChangePasswordAsync)
            .WithName("ChangePassword")
            .Produces<ChangePasswordResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("LogoutEverywhere", LogoutEverywhereAsync)
            .WithName("LogoutEverywhere")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return api;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest registerRequest,
        [FromServices] IAccountService accountService)
    {
        Result<RegisterResponse> result = await accountService.RegisterAsync(registerRequest);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest loginRequest,
        [FromServices] IAccountService accountService)
    {
        Result<LoginResponse> result = await accountService.LoginAsync(loginRequest);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest changePasswordRequest,
        [FromServices] IAccountService accountService)
    {
        Result<ChangePasswordResponse> result = await accountService.ChangePasswordAsync(changePasswordRequest);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> LogoutEverywhereAsync([FromServices] IAccountService accountService)
    {
        await accountService.LogoutEverywhereAsync();
        return TypedResults.NoContent();
    }
}

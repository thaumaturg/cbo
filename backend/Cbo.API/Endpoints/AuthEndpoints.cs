using System.Net.Mime;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.DTO;
using Cbo.API.Services;
using Cbo.Results;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder auth = api.MapGroup("Auth").WithTags("Auth");

        auth.MapPost("Register", RegisterAsync)
            .WithName("Register")
            .AllowAnonymous()
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Plain)
            .ProducesValidationProblem()
            .ProducesValidationProblem(StatusCodes.Status409Conflict);

        auth.MapPost("Login", LoginAsync)
            .WithName("Login")
            .AllowAnonymous()
            .Produces<LoginResponseDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("ChangePassword", ChangePasswordAsync)
            .WithName("ChangePassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return api;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUserDto registerUserDto,
        [FromServices] IAuthService authService)
    {
        Result result = await authService.RegisterAsync(registerUserDto);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.Text("Account created. You can now log in.");
    }

    private static async Task<IResult> LoginAsync(
        LoginUserDto loginUserDto,
        [FromServices] IAuthService authService)
    {
        Result<LoginResponseDto> result = await authService.LoginAsync(loginUserDto);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Failure.ToProblem();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordDto changePasswordDto,
        [FromServices] IAuthService authService)
    {
        Result result = await authService.ChangePasswordAsync(changePasswordDto);
        if (result.IsFailure)
            return result.Failure.ToProblem();

        return TypedResults.NoContent();
    }
}

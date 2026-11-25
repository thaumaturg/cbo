using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Cbo.Api.Validation;
using Cbo.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cbo.Tests.Validation;

/// <summary>
/// Calls endpoints in a group that has <see cref="ValidationFilter"/>, the way <c>Program</c> sets up <c>/api</c>, so
/// the arguments are bound from real requests.
/// </summary>
public sealed class ValidationFilterTests : IAsyncDisposable
{
    public sealed class TestItem
    {
        public string Name { get; set; } = "";
    }

    public sealed class TestBody
    {
        public string Title { get; set; } = "";

        public List<TestItem> Items { get; set; } = [];
    }

    public sealed class TestQuery
    {
        public int? PageSize { get; set; }
    }

    public sealed class UnvalidatedBody
    {
        public string Title { get; set; } = "";
    }

    public sealed class TestItemValidator : ChildValidator<TestItem>
    {
        public TestItemValidator()
        {
            RuleFor(i => i.Name).NotEmpty().WithMessage("The name is required.");
        }
    }

    public sealed class TestBodyValidator : RequestValidator<TestBody>
    {
        public TestBodyValidator()
        {
            RuleFor(b => b.Title).NotEmpty().WithMessage("The title is required.");
            RuleForEach(b => b.Items).SetValidator(new TestItemValidator());
        }

        public override string InvalidMessage => "Some of the test data is invalid.";
    }

    // A root RuleForEach would name the paths after the lambda parameter (items[1].name), so bare-list paths are added by hand
    public sealed class TestListValidator : RequestValidator<List<TestItem>>
    {
        public TestListValidator()
        {
            RuleFor(items => items).Custom((items, context) =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Name.Length == 0)
                        context.AddFailure($"[{i}].name", "The name is required.");
                }
            });
        }

        public override string InvalidMessage => "Some of the test items are invalid.";
    }

    public sealed class TestQueryValidator : RequestValidator<TestQuery>
    {
        public TestQueryValidator()
        {
            RuleFor(q => q.PageSize).InclusiveBetween(1, 50).WithMessage("The page size must be between 1 and 50.");
        }

        public override string InvalidMessage => "Some of the list parameters are invalid.";
    }

    private readonly WebApplication _app;
    private readonly Dictionary<string, Endpoint> _endpoints;
    private int _handlerCalls;

    public ValidationFilterTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddProblemDetails();
        builder.Services.AddScoped<IValidator<TestBody>, TestBodyValidator>();
        builder.Services.AddScoped<IValidator<List<TestItem>>, TestListValidator>();
        builder.Services.AddScoped<IValidator<TestQuery>, TestQueryValidator>();
        _app = builder.Build();

        RouteGroupBuilder api = _app.MapGroup("/api").AddEndpointFilter<ValidationFilter>();
        api.MapPost("/body", (TestBody body) => Handled());
        api.MapPost("/list", (List<TestItem> items) => Handled());
        api.MapGet("/query", ([AsParameters] TestQuery query) => Handled());
        api.MapPost("/unvalidated", (UnvalidatedBody body) => Handled());

        _endpoints = ((IEndpointRouteBuilder)_app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToDictionary(endpoint => endpoint.RoutePattern.RawText!, endpoint => (Endpoint)endpoint);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();

    [Fact]
    public async Task An_invalid_body_gets_a_400_problem_with_the_validators_message_and_errors_by_path_and_the_handler_is_not_called()
    {
        (int status, JsonElement problem) = await SendAsync("/api/body", body: """{"title":"","items":[{"name":"a"},{"name":""}]}""");

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("Some of the test data is invalid.", problem.GetProperty("detail").GetString());
        Assert.Equal(
            new Dictionary<string, string[]>
            {
                ["title"] = ["The title is required."],
                ["items[1].name"] = ["The name is required."],
            },
            ReadErrors(problem));
        Assert.Equal(0, _handlerCalls);
    }

    [Fact]
    public async Task A_valid_body_reaches_the_handler()
    {
        (int status, _) = await SendAsync("/api/body", body: """{"title":"History","items":[{"name":"a"}]}""");

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(1, _handlerCalls);
    }

    [Fact]
    public async Task An_invalid_bare_list_body_gets_errors_keyed_by_index()
    {
        (int status, JsonElement problem) = await SendAsync("/api/list", body: """[{"name":"a"},{"name":""}]""");

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Some of the test items are invalid.", problem.GetProperty("detail").GetString());
        Assert.Equal(
            new Dictionary<string, string[]> { ["[1].name"] = ["The name is required."] },
            ReadErrors(problem));
        Assert.Equal(0, _handlerCalls);
    }

    [Fact]
    public async Task An_invalid_query_object_gets_a_400_problem()
    {
        (int status, JsonElement problem) = await SendAsync("/api/query", query: "?pageSize=51");

        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("Some of the list parameters are invalid.", problem.GetProperty("detail").GetString());
        Assert.Equal(
            new Dictionary<string, string[]> { ["pageSize"] = ["The page size must be between 1 and 50."] },
            ReadErrors(problem));
        Assert.Equal(0, _handlerCalls);
    }

    [Fact]
    public async Task A_valid_query_object_reaches_the_handler()
    {
        (int status, _) = await SendAsync("/api/query", query: "?pageSize=50");

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(1, _handlerCalls);
    }

    [Fact]
    public async Task An_argument_without_a_validator_reaches_the_handler()
    {
        (int status, _) = await SendAsync("/api/unvalidated", body: """{"title":""}""");

        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.Equal(1, _handlerCalls);
    }

    private IResult Handled()
    {
        _handlerCalls++;
        return TypedResults.Ok();
    }

    private async Task<(int Status, JsonElement Problem)> SendAsync(string path, string? body = null, string query = "")
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Method = body is null ? HttpMethods.Get : HttpMethods.Post;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        if (body is not null)
        {
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestWithBody());
            context.Request.ContentType = "application/json";
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _endpoints[path].RequestDelegate!(context);

        responseBody.Position = 0;
        JsonElement problem = responseBody.Length == 0
            ? default
            : (await JsonDocument.ParseAsync(responseBody)).RootElement.Clone();

        return (context.Response.StatusCode, problem);
    }

    private static Dictionary<string, string[]> ReadErrors(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().ToDictionary(
            field => field.Name,
            field => field.Value.EnumerateArray().Select(message => message.GetString()!).ToArray());

    private sealed class RequestWithBody : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}

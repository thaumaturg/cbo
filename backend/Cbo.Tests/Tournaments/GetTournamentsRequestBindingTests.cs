using System.Threading.Tasks;
using Cbo.Core.Domain;
using Cbo.Core.Tournaments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cbo.Tests.Tournaments;

// The stage filter is an enum, so Minimal APIs reject an unknown name while binding, before the validator runs
public class GetTournamentsRequestBindingTests
{
    private static async Task<GetTournamentsRequest?> BindAsync(string queryString)
    {
        GetTournamentsRequest? bound = null;
        ServiceProvider services = new ServiceCollection().AddLogging().BuildServiceProvider();

        RequestDelegateResult handler = RequestDelegateFactory.Create(
            ([AsParameters] GetTournamentsRequest request) => { bound = request; },
            new RequestDelegateFactoryOptions { ServiceProvider = services, ThrowOnBadRequest = true });

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.QueryString = new QueryString(queryString);
        await handler.RequestDelegate(context);

        return bound;
    }

    [Theory]
    [InlineData("Preparations", TournamentStage.Preparations)]
    [InlineData("Qualifications", TournamentStage.Qualifications)]
    [InlineData("Playoff", TournamentStage.Playoff)]
    [InlineData("Ended", TournamentStage.Ended)]
    public async Task Every_stage_name_binds_as_the_stage_filter(string stageName, TournamentStage expected)
    {
        GetTournamentsRequest? request = await BindAsync($"?stage={stageName}");

        Assert.Equal(expected, request?.Stage);
    }

    [Fact]
    public async Task The_old_started_filter_is_a_bad_request()
    {
        BadHttpRequestException exception =
            await Assert.ThrowsAsync<BadHttpRequestException>(() => BindAsync("?stage=Started"));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }
}

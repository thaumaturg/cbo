using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Cbo.Tests.Integration;

public class ChangeStageTests(ApiFactory api)
{
    [Fact]
    public async Task An_unknown_stage_is_a_validation_error()
    {
        TestTournament tournament = await TestTournament.CreateAsync(
            api, ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"]);

        HttpResponseMessage response = await tournament.Creator.PatchAsJsonAsync(
            $"/api/Tournaments/{tournament.Id}", new { stage = "Started" }, TestContext.Current.CancellationToken);

        await TestTournament.FailedWith(HttpStatusCode.BadRequest, response);
    }
}

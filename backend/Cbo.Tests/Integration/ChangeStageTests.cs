using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cbo.Core.Domain;

namespace Cbo.Tests.Integration;

public class ChangeStageTests(ApiFactory api)
{
    [Fact]
    public async Task The_stage_cant_be_set_to_preparations()
    {
        TestTournament tournament = await TestTournament.CreateAsync(
            api, ["S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "S9", "S10", "S11", "S12"]);

        HttpResponseMessage response = await tournament.ChangeStageAsync(TournamentStage.Preparations);

        await TestTournament.FailedWith(HttpStatusCode.BadRequest, response);
    }
}

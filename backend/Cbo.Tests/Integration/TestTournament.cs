using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cbo.Core.Accounts;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Cbo.Core.Matches;
using Cbo.Core.Rounds;
using Cbo.Core.Tournaments;
using Microsoft.Extensions.DependencyInjection;

namespace Cbo.Tests.Integration;

/// <summary>
/// A tournament in its own accounts, set up in the database ready for Start Qualifications, then played through the
/// API. Its Creator and an Organizer bring enough topics for every round. Players are named by the caller and seeded
/// in the order given.
/// </summary>
internal sealed class TestTournament
{
    public const int StaffTopicCount = 50;

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Queue<(Guid TopicId, Guid QuestionId)> _unplayedTopics;

    private TestTournament(Guid id, HttpClient creator, HttpClient player, IEnumerable<(Guid, Guid)> topics)
    {
        Id = id;
        Creator = creator;
        Player = player;
        _unplayedTopics = new Queue<(Guid, Guid)>(topics);
    }

    public Guid Id { get; }

    /// <summary>The Creator's client.</summary>
    public HttpClient Creator { get; }

    /// <summary>The first player's client.</summary>
    public HttpClient Player { get; }

    /// <summary>
    /// Sets up a tournament in Preparations with these players, seeded in this order. Player names must be at most
    /// 10 characters, letters and digits only.
    /// </summary>
    public static async Task<TestTournament> CreateAsync(
        ApiFactory api, IReadOnlyList<string> playersBySeed, TournamentFormat format = TournamentFormat.TwelveShort)
    {
        api.SkipUnlessAvailable();

        string suffix = Guid.NewGuid().ToString("N")[..5];
        using IServiceScope scope = api.Services.CreateScope();
        CboDbContext dbContext = scope.ServiceProvider.GetRequiredService<CboDbContext>();
        IJwtTokenService tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        ApplicationUser creator = Account($"creator_{suffix}");
        ApplicationUser organizer = Account($"organizer_{suffix}");
        List<ApplicationUser> players = [.. playersBySeed.Select(name => Account($"{name}_{suffix}"))];
        dbContext.Users.AddRange([creator, organizer, .. players]);

        var tournament = new Tournament
        {
            Title = $"Playoff {suffix}",
            CurrentStage = TournamentStage.Preparations,
            Format = format,
            TopicsPerParticipantMax = StaffTopicCount,
            TopicsPerParticipantMin = 0,
        };
        dbContext.Tournaments.Add(tournament);

        TournamentParticipant creatorParticipant = Participant(tournament, creator, TournamentParticipantRole.Creator);
        TournamentParticipant organizerParticipant = Participant(tournament, organizer, TournamentParticipantRole.Organizer);
        dbContext.TournamentParticipants.AddRange(creatorParticipant, organizerParticipant);
        dbContext.TournamentParticipants.AddRange(players.Select((player, index) =>
        {
            TournamentParticipant participant = Participant(tournament, player, TournamentParticipantRole.Player);
            participant.Seed = index + 1;
            return participant;
        }));

        List<(Guid TopicId, Guid QuestionId)> topics = [];
        foreach ((ApplicationUser staff, TournamentParticipant participant) in new[] { (creator, creatorParticipant), (organizer, organizerParticipant) })
        {
            for (int priority = 0; priority < StaffTopicCount; priority++)
            {
                var topic = new Topic { Id = Guid.NewGuid(), Title = $"{staff.UserName} {priority}" };
                var question = new Question { Id = Guid.NewGuid(), QuestionNumber = 1, Cost = 10, Text = "Question", Answer = "Answer", TopicId = topic.Id };
                topic.Questions.Add(question);
                topic.TopicAuthors.Add(new TopicAuthor { ApplicationUserId = staff.Id, TopicId = topic.Id, IsOwner = true, IsAuthor = true });
                dbContext.Topics.Add(topic);
                dbContext.TournamentTopics.Add(new TournamentTopic
                {
                    PriorityIndex = priority,
                    TournamentId = tournament.Id,
                    TopicId = topic.Id,
                    TournamentParticipant = participant,
                    TournamentParticipantId = participant.Id,
                });
                topics.Add((topic.Id, question.Id));
            }
        }

        await dbContext.SaveChangesAsync();

        return new TestTournament(
            tournament.Id, Client(api, tokens.CreateToken(creator)), Client(api, tokens.CreateToken(players[0])), topics);
    }

    /// <summary>The name a player was created with.</summary>
    public static string NameOf(MatchParticipantResponse participant) => participant.Username[..participant.Username.LastIndexOf('_')];

    public Task<HttpResponseMessage> ChangeStageAsync(TournamentStage stage, HttpClient? client = null) =>
        (client ?? Creator).PatchAsJsonAsync($"/api/Tournaments/{Id}", new { stage }, JsonOptions);

    public async Task StartQualificationsAsync() => await Succeeded(await ChangeStageAsync(TournamentStage.Qualifications));

    /// <summary>Plays every qualification match. Each player scores the same in every match: 0 unless given.</summary>
    public async Task PlayQualificationsAsync(IReadOnlyDictionary<string, int>? scoreOf = null)
    {
        foreach (MatchResponse match in (await MatchesAsync()).Where(m => m.Type == MatchType.Qualification))
        {
            await PlayAsync(match, match.MatchParticipants.ToDictionary(NameOf, p => scoreOf?.GetValueOrDefault(NameOf(p)) ?? 0));
        }
    }

    /// <summary>Starts Qualifications, plays them with no scores, so qualification places follow the seeds, and starts the Playoff.</summary>
    public async Task StartPlayoffWithPlacesBySeedAsync()
    {
        await StartQualificationsAsync();
        await PlayQualificationsAsync();
        await Succeeded(await ChangeStageAsync(TournamentStage.Playoff));
    }

    public async Task<TournamentResponse> GetAsync() =>
        (await Creator.GetFromJsonAsync<TournamentResponse>($"/api/Tournaments/{Id}", JsonOptions))!;

    public async Task<List<MatchResponse>> MatchesAsync() =>
        (await Creator.GetFromJsonAsync<List<MatchResponse>>($"/api/Tournaments/{Id}/matches", JsonOptions))!;

    public async Task<MatchDetailResponse> MatchAsync(Guid matchId) =>
        (await Creator.GetFromJsonAsync<MatchDetailResponse>($"/api/Tournaments/{Id}/matches/{matchId}", JsonOptions))!;

    public async Task<MatchResponse> MatchAsync(BracketPosition position) =>
        (await MatchesAsync()).Single(m => m.BracketPosition == position);

    /// <summary>Plays every round of the match: each player scores their score in the first round and nothing after.</summary>
    public async Task PlayAsync(MatchResponse match, IReadOnlyDictionary<string, int> scores)
    {
        for (int round = 1; round <= match.Type.RoundsToPlay(); round++)
        {
            (Guid topicId, Guid questionId) = _unplayedTopics.Dequeue();
            var request = new CreateRoundRequest
            {
                NumberInMatch = round,
                TopicId = topicId,
                IsOverrideMode = round == 1,
                Answers = round == 1
                    ? [.. match.MatchParticipants.Select(p => new CreateRoundAnswerRequest
                    {
                        QuestionId = questionId,
                        MatchParticipantId = p.Id,
                        OverrideCost = scores[NameOf(p)],
                    })]
                    : [],
            };

            await Succeeded(await Creator.PostAsJsonAsync($"/api/Tournaments/{Id}/matches/{match.Id}/rounds", request, JsonOptions));
        }
    }

    public Task<HttpResponseMessage> ConfirmAsync(Guid matchId, HttpClient? client = null) =>
        (client ?? Creator).PostAsync($"/api/Tournaments/{Id}/matches/{matchId}/confirm", content: null);

    /// <summary>Plays the existing match at this bracket position with these scores and confirms it.</summary>
    public async Task<MatchDetailResponse> PlayAndConfirmAsync(BracketPosition position, IReadOnlyDictionary<string, int> scores) =>
        await PlayAndConfirmAsync(await MatchAsync(position), scores);

    public async Task<MatchDetailResponse> PlayAndConfirmAsync(MatchResponse match, IReadOnlyDictionary<string, int> scores)
    {
        await PlayAsync(match, scores);
        HttpResponseMessage confirmed = await ConfirmAsync(match.Id);
        await Succeeded(confirmed);
        return (await confirmed.Content.ReadFromJsonAsync<MatchDetailResponse>(JsonOptions))!;
    }

    /// <summary>Fails with the response body unless the request succeeded.</summary>
    public static async Task Succeeded(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} gave {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>Asserts the request failed with this status, and returns the problem's detail.</summary>
    public static async Task<string> FailedWith(HttpStatusCode status, HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {body}");
        return JsonDocument.Parse(body).RootElement.TryGetProperty("detail", out JsonElement detail) ? detail.GetString() ?? string.Empty : string.Empty;
    }

    private static ApplicationUser Account(string userName) => new()
    {
        Id = Guid.NewGuid(),
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = $"{userName}@example.com",
        NormalizedEmail = $"{userName}@example.com".ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString(),
    };

    private static TournamentParticipant Participant(Tournament tournament, ApplicationUser user, TournamentParticipantRole role) => new()
    {
        Id = Guid.NewGuid(),
        Role = role,
        TournamentId = tournament.Id,
        Tournament = tournament,
        ApplicationUserId = user.Id,
    };

    private static HttpClient Client(ApiFactory api, string token)
    {
        HttpClient client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

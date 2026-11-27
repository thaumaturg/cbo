using System;
using System.Collections.Generic;
using System.Linq;
using Cbo.Core.Matches;

namespace Cbo.Tests.Playoffs;

internal static class PlayoffScores
{
    /// <summary>Each player's score, with a positive score that counts only what they won.</summary>
    public static Dictionary<string, AnswerScores> Of(params (string Player, int Score)[] scores) =>
        scores.ToDictionary(entry => entry.Player, entry => new AnswerScores(entry.Score, Math.Max(entry.Score, 0)));
}

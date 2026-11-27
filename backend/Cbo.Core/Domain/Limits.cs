using System.Collections.Immutable;

namespace Cbo.Core.Domain;

/// <summary>
/// Domain limits, one nested class per area. The frontend gets them at build time, see
/// docs/adr/0010-domain-limits-live-in-code-and-ship-to-the-frontend-at-build-time.md.
/// </summary>
public static class Limits
{
    public static class Topics
    {
        /// <summary>Most characters in a topic title.</summary>
        public const int TitleLengthMax = 100;

        /// <summary>Most characters in a topic description.</summary>
        public const int DescriptionLengthMax = 500;

        /// <summary>Fewest questions a topic can have.</summary>
        public const int QuestionsMin = 1;

        /// <summary>Most questions a topic can have.</summary>
        public const int QuestionsMax = 10;

        /// <summary>Most characters in a question's text.</summary>
        public const int QuestionTextLengthMax = 4000;

        /// <summary>Most characters in a question's answer.</summary>
        public const int AnswerLengthMax = 4000;

        /// <summary>Most characters in a question's comment.</summary>
        public const int CommentLengthMax = 4000;

        /// <summary>Lowest cost of a single question.</summary>
        public const int QuestionCostMin = 1;

        /// <summary>Highest cost of a single question.</summary>
        public const int QuestionCostMax = 150;

        /// <summary>Highest total cost of all of a topic's questions.</summary>
        public const int QuestionsCostTotalMax = 150;

        /// <summary>
        /// Most co-authors a topic can credit. The owner is never counted, even when they credit themselves as an
        /// author.
        /// </summary>
        public const int CoAuthorsMax = 10;

        /// <summary>Most topics one account can own.</summary>
        public const int OwnedMax = 1000;

        /// <summary>Most topics one import can create.</summary>
        public const int ImportMax = 50;
    }

    public static class Tournaments
    {
        /// <summary>
        /// Formats a Creator may choose. Only formats with a playoff bracket are selectable, see
        /// docs/adr/0013-format-replaces-tournament-size-as-the-setting.md.
        /// </summary>
        public static readonly ImmutableArray<TournamentFormat> SelectableFormats =
            [TournamentFormat.TwelveShort, TournamentFormat.TwelveLong];

        /// <summary>Fewest characters in a tournament title.</summary>
        public const int TitleLengthMin = 3;

        /// <summary>Most characters in a tournament title.</summary>
        public const int TitleLengthMax = 100;

        /// <summary>Most characters in a tournament description.</summary>
        public const int DescriptionLengthMax = 500;

        /// <summary>Lowest minimum or maximum number of topics per participant a Creator may set.</summary>
        public const int TopicsPerParticipantFloor = 0;

        /// <summary>Highest maximum number of topics per participant a Creator may set.</summary>
        public const int TopicsPerParticipantCeiling = 50;
    }

    public static class Matches
    {
        /// <summary>Fewest participants in a match.</summary>
        public const int ParticipantsMin = 2;

        /// <summary>Most participants in a match.</summary>
        public const int ParticipantsMax = 4;

        /// <summary>Rounds in a qualification match. A match with all of them played is finished.</summary>
        public const int RoundsPerQualificationMatch = 4;
    }

    public static class Rounds
    {
        /// <summary>Lowest override cost of an answer.</summary>
        public const int OverrideCostMin = -9999;

        /// <summary>Highest override cost of an answer.</summary>
        public const int OverrideCostMax = 9999;
    }

    public static class Accounts
    {
        /// <summary>Fewest characters in a username.</summary>
        public const int UsernameLengthMin = 3;

        /// <summary>Most characters in a username.</summary>
        public const int UsernameLengthMax = 16;

        /// <summary>
        /// The only characters a username may contain: Latin letters, digits and underscores. Drives Identity's
        /// <c>AllowedUserNameCharacters</c> and is shared with the frontend.
        /// </summary>
        public const string UsernameAllowedCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

        /// <summary>Most characters in an email. Matches the column ASP.NET Core Identity creates.</summary>
        public const int EmailLengthMax = 256;

        /// <summary>Fewest characters in a password.</summary>
        public const int PasswordLengthMin = 8;

        /// <summary>Most characters in a password.</summary>
        public const int PasswordLengthMax = 64;

        /// <summary>Fewest characters in a full name.</summary>
        public const int FullNameLengthMin = 2;

        /// <summary>Most characters in a full name.</summary>
        public const int FullNameLengthMax = 64;

        /// <summary>
        /// Characters a full name may contain: ASCII letters, Latin Extended-A (which covers the Latvian letters),
        /// Cyrillic А–я, Ё and ё, a plain space, the apostrophes ' and ’, and a hyphen. No other whitespace. The
        /// <c>\uXXXX</c> escapes stay literal, so .NET <c>Regex</c> and JavaScript <c>new RegExp(pattern, "u")</c>
        /// read the pattern the same way. Shared with the frontend.
        /// </summary>
        public const string FullNamePattern = @"^[A-Za-z\u0100-\u017E\u0401\u0410-\u044F\u0451 '\u2019-]+$";
    }
}

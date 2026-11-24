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
        /// <summary>
        /// Most characters in a topic title. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int TitleLengthMax = 100;

        /// <summary>Fewest questions a topic can have.</summary>
        public const int QuestionsMin = 1;

        /// <summary>Most questions a topic can have.</summary>
        public const int QuestionsMax = 10;

        /// <summary>Lowest cost of a single question.</summary>
        public const int QuestionCostMin = 1;

        /// <summary>Highest cost of a single question.</summary>
        public const int QuestionCostMax = 150;

        /// <summary>Highest total cost of all of a topic's questions.</summary>
        public const int QuestionsCostTotalMax = 150;

        /// <summary>
        /// Most co-authors a topic can credit. The owner is never counted, even when they credit themselves as an
        /// author. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
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
        /// Tournament sizes a Creator may choose. Each size needs a qualification table in
        /// <see cref="Cbo.Core.Matches.MatchGenerationService"/>.
        /// </summary>
        public static readonly ImmutableArray<int> SupportedSizes = [14];

        /// <summary>
        /// Fewest characters in a tournament title. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int TitleLengthMin = 3;

        /// <summary>
        /// Most characters in a tournament title. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int TitleLengthMax = 100;

        /// <summary>
        /// Most characters in a tournament description. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int DescriptionLengthMax = 500;

        /// <summary>Lowest minimum or maximum number of topics per participant a Creator may set.</summary>
        public const int TopicsPerParticipantFloor = 0;

        /// <summary>
        /// Highest minimum or maximum number of topics per participant a Creator may set. Not enforced yet; see
        /// docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int TopicsPerParticipantCeiling = 20;
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
        /// <summary>
        /// Lowest override cost of an answer. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int OverrideCostMin = -999;

        /// <summary>
        /// Highest override cost of an answer. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int OverrideCostMax = 999;
    }

    public static class Accounts
    {
        /// <summary>
        /// Fewest characters in a username. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int UsernameLengthMin = 3;

        /// <summary>
        /// Most characters in a username. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int UsernameLengthMax = 16;

        /// <summary>Fewest characters in a password.</summary>
        public const int PasswordLengthMin = 8;

        /// <summary>
        /// Most characters in a password. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int PasswordLengthMax = 64;

        /// <summary>
        /// Fewest characters in a full name. Not enforced yet; see docs/specs/0001-shared-domain-limits.md.
        /// </summary>
        public const int FullNameLengthMin = 2;

        /// <summary>Most characters in a full name.</summary>
        public const int FullNameLengthMax = 64;
    }
}

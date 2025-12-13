using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents a serializable definition for an expedition, including its identity and gameplay parameters.
    /// </summary>
    public class ExpeditionDefinition
    {
        /// <summary>
        /// Unique identifier for the expedition used for lookups and persistence.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Player-facing name displayed in UI elements and logs.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Duration of the expedition in game ticks.
        /// </summary>
        public int DurationTicks { get; }

        /// <summary>
        /// Numeric difficulty rating used for balancing and matchmaking.
        /// </summary>
        public int Difficulty { get; }

        /// <summary>
        /// Minimum player progression level required to start the expedition.
        /// </summary>
        public int MinPlayerLevel { get; }

        /// <summary>
        /// Indicates whether the expedition can be repeated after completion.
        /// </summary>
        public bool IsRepeatable { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExpeditionDefinition"/> with the provided values.
        /// </summary>
        /// <param name="id">Unique identifier for the expedition.</param>
        /// <param name="displayName">Player-facing name displayed in UI elements and logs.</param>
        /// <param name="durationTicks">Duration of the expedition in game ticks.</param>
        /// <param name="difficulty">Numeric difficulty rating used for balancing and matchmaking.</param>
        /// <param name="minPlayerLevel">Minimum player progression level required to start the expedition.</param>
        /// <param name="isRepeatable">Whether the expedition can be repeated after completion.</param>
        public ExpeditionDefinition(string id, string displayName, int durationTicks, int difficulty, int minPlayerLevel, bool isRepeatable)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DurationTicks = durationTicks;
            Difficulty = difficulty;
            MinPlayerLevel = minPlayerLevel;
            IsRepeatable = isRepeatable;
        }
    }
}

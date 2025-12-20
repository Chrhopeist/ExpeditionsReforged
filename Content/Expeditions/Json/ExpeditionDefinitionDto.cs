using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// JSON-facing representation of an expedition definition.
    /// </summary>
    public sealed class ExpeditionDefinitionDto
    {
        /// <summary>
        /// Unique identifier for the expedition used for lookups and persistence.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Localization key for the player-facing expedition name.
        /// </summary>
        public string DisplayNameKey { get; set; } = string.Empty;

        /// <summary>
        /// Localization key for the descriptive flavor text.
        /// </summary>
        public string DescriptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Categorical grouping for UI filtering and balance.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Rarity tier of the expedition used by selection and UI coloring.
        /// </summary>
        public int Rarity { get; set; }

        /// <summary>
        /// Duration of the expedition in game ticks.
        /// </summary>
        public int DurationTicks { get; set; }

        /// <summary>
        /// Numeric difficulty rating used for balancing and matchmaking.
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// Minimum player progression tier required to start the expedition.
        /// </summary>
        public string MinProgressionTier { get; set; } = string.Empty;

        /// <summary>
        /// Legacy integer minimum player level field kept for backward-compatible JSON imports.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MinPlayerLevel { get; set; }

        /// <summary>
        /// Indicates whether the expedition can be repeated after completion.
        /// </summary>
        public bool IsRepeatable { get; set; }

        /// <summary>
        /// Indicates whether the expedition can be rolled as a daily contract.
        /// </summary>
        public bool IsDailyEligible { get; set; }

        /// <summary>
        /// The head icon ID for the NPC patronizing the expedition.
        /// </summary>
        public int NpcHeadId { get; set; }

        /// <summary>
        /// Ordered list of prerequisite conditions that must be satisfied to start the expedition.
        /// </summary>
        public List<ConditionDefinitionDto> Prerequisites { get; set; } = new();

        /// <summary>
        /// Deliverables that must be provided by the player while the expedition is active.
        /// </summary>
        public List<DeliverableDefinitionDto> Deliverables { get; set; } = new();

        /// <summary>
        /// Rewards granted on completion.
        /// </summary>
        public List<RewardDefinitionDto> Rewards { get; set; } = new();

        /// <summary>
        /// Supplemental rewards awarded when the expedition is selected as a daily contract.
        /// </summary>
        public List<RewardDefinitionDto> DailyRewards { get; set; } = new();
    }
}

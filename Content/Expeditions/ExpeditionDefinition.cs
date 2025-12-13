using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
        /// The category or biome grouping that the expedition belongs to.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Rarity tier of the expedition used by selection and UI coloring.
        /// </summary>
        public int Rarity { get; }

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
        /// Indicates whether the expedition can be rolled as a daily contract.
        /// </summary>
        public bool IsDailyEligible { get; }

        /// <summary>
        /// The head icon ID for the NPC patronizing the expedition.
        /// </summary>
        public int NpcHeadId { get; }

        /// <summary>
        /// Ordered list of prerequisite conditions that must be satisfied to start the expedition.
        /// </summary>
        public IReadOnlyList<ConditionDefinition> Prerequisites { get; }

        /// <summary>
        /// Deliverables that must be provided by the player while the expedition is active.
        /// </summary>
        public IReadOnlyList<DeliverableDefinition> Deliverables { get; }

        /// <summary>
        /// Rewards granted on completion. These entries are guaranteed unless <see cref="RewardDefinition.DropChance"/> is used.
        /// </summary>
        public IReadOnlyList<RewardDefinition> Rewards { get; }

        /// <summary>
        /// Supplemental rewards awarded when the expedition is selected as a daily contract.
        /// </summary>
        public IReadOnlyList<RewardDefinition> DailyRewards { get; }

        /// <summary>
        /// A stable hash derived from the content definition that can be used to validate persisted progress across versions.
        /// </summary>
        public string ContentHash { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ExpeditionDefinition"/> with the provided values.
        /// </summary>
        /// <param name="id">Unique identifier for the expedition.</param>
        /// <param name="displayName">Player-facing name displayed in UI elements and logs.</param>
        /// <param name="category">High-level grouping for UI filtering and progression.</param>
        /// <param name="rarity">Rarity tier used by selection logic.</param>
        /// <param name="durationTicks">Duration of the expedition in game ticks.</param>
        /// <param name="difficulty">Numeric difficulty rating used for balancing and matchmaking.</param>
        /// <param name="minPlayerLevel">Minimum player progression level required to start the expedition.</param>
        /// <param name="isRepeatable">Whether the expedition can be repeated after completion.</param>
        /// <param name="isDailyEligible">Whether the expedition can appear as a daily contract.</param>
        /// <param name="npcHeadId">Head icon ID that represents the quest giver.</param>
        /// <param name="prerequisites">Optional set of prerequisite conditions.</param>
        /// <param name="deliverables">Optional set of deliverables required to complete the expedition.</param>
        /// <param name="rewards">Reward list granted on completion.</param>
        /// <param name="dailyRewards">Additional rewards when selected as daily content.</param>
        public ExpeditionDefinition(
            string id,
            string displayName,
            string category,
            int rarity,
            int durationTicks,
            int difficulty,
            int minPlayerLevel,
            bool isRepeatable,
            bool isDailyEligible,
            int npcHeadId,
            IEnumerable<ConditionDefinition>? prerequisites = null,
            IEnumerable<DeliverableDefinition>? deliverables = null,
            IEnumerable<RewardDefinition>? rewards = null,
            IEnumerable<RewardDefinition>? dailyRewards = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Category = category ?? string.Empty;
            Rarity = rarity;
            DurationTicks = durationTicks;
            Difficulty = difficulty;
            MinPlayerLevel = minPlayerLevel;
            IsRepeatable = isRepeatable;
            IsDailyEligible = isDailyEligible;
            NpcHeadId = npcHeadId;
            Prerequisites = new ReadOnlyCollection<ConditionDefinition>((prerequisites ?? Enumerable.Empty<ConditionDefinition>()).ToList());
            Deliverables = new ReadOnlyCollection<DeliverableDefinition>((deliverables ?? Enumerable.Empty<DeliverableDefinition>()).ToList());
            Rewards = new ReadOnlyCollection<RewardDefinition>((rewards ?? Enumerable.Empty<RewardDefinition>()).ToList());
            DailyRewards = new ReadOnlyCollection<RewardDefinition>((dailyRewards ?? Enumerable.Empty<RewardDefinition>()).ToList());
            ContentHash = ComputeContentHash();
        }

        /// <summary>
        /// Creates a deep copy of the expedition definition so that per-player state can be attached without mutating shared data.
        /// </summary>
        public ExpeditionDefinition Clone()
        {
            return new ExpeditionDefinition(
                id: Id,
                displayName: DisplayName,
                category: Category,
                rarity: Rarity,
                durationTicks: DurationTicks,
                difficulty: Difficulty,
                minPlayerLevel: MinPlayerLevel,
                isRepeatable: IsRepeatable,
                isDailyEligible: IsDailyEligible,
                npcHeadId: NpcHeadId,
                prerequisites: Prerequisites.Select(p => p.Clone()).ToList(),
                deliverables: Deliverables.Select(d => d.Clone()).ToList(),
                rewards: Rewards.Select(r => r.Clone()).ToList(),
                dailyRewards: DailyRewards.Select(r => r.Clone()).ToList());
        }

        /// <summary>
        /// Generates a stable, deterministic hash for the expedition that can be combined with per-player salts to preserve save data.
        /// </summary>
        public string GetStableProgressKey(int playerId)
        {
            using var sha256 = SHA256.Create();
            var raw = Encoding.UTF8.GetBytes($"{Id}|{ContentHash}|{playerId}");
            var hash = sha256.ComputeHash(raw);
            return Convert.ToHexString(hash);
        }

        private string ComputeContentHash()
        {
            using var sha256 = SHA256.Create();
            var builder = new StringBuilder();
            builder.Append(Id).Append('|').Append(DisplayName).Append('|').Append(Category)
                .Append('|').Append(Rarity).Append('|').Append(DurationTicks).Append('|').Append(Difficulty)
                .Append('|').Append(MinPlayerLevel).Append('|').Append(IsRepeatable).Append('|').Append(IsDailyEligible)
                .Append('|').Append(NpcHeadId);

            foreach (var prerequisite in Prerequisites)
            {
                builder.Append("|pre:").Append(prerequisite.SerializeForHash());
            }

            foreach (var deliverable in Deliverables)
            {
                builder.Append("|deliv:").Append(deliverable.SerializeForHash());
            }

            foreach (var reward in Rewards)
            {
                builder.Append("|rw:").Append(reward.SerializeForHash());
            }

            foreach (var reward in DailyRewards)
            {
                builder.Append("|drw:").Append(reward.SerializeForHash());
            }

            var bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}

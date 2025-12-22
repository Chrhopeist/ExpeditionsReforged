using System;
using System.Collections.Generic;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents the progress state of an expedition for a single player.
    /// Designed as a pure data model without direct dependencies on player logic.
    /// </summary>
    public sealed class ExpeditionProgress
    {
        /// <summary>
        /// Gets or sets the unique identifier for the expedition definition.
        /// </summary>
        public string ExpeditionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stable, per-player progress key used for persistence.
        /// </summary>
        public string StableProgressKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the game tick when the expedition was started.
        /// </summary>
        public long StartGameTick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the expedition objectives are complete.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the expedition is currently active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rewards for this expedition have been claimed.
        /// </summary>
        public bool RewardsClaimed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the definition backing this progress entry is missing.
        /// </summary>
        public bool IsOrphaned { get; set; }

        /// <summary>
        /// Per-condition progress counters keyed by condition identifier.
        /// </summary>
        public Dictionary<string, int> ConditionProgress { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Marks the expedition as completed.
        /// </summary>
        public void Complete()
        {
            // Keep the expedition active until rewards are claimed so turn-in NPC logic can verify ownership.
            IsCompleted = true;
        }

        /// <summary>
        /// Marks the expedition's rewards as claimed.
        /// </summary>
        public void ClaimRewards()
        {
            RewardsClaimed = true;
            // Once rewards are claimed, the expedition no longer participates in active gameplay.
            IsActive = false;
        }

        /// <summary>
        /// Creates a new progress entry using the stable per-player hash generated from the provided definition.
        /// </summary>
        public static ExpeditionProgress Create(ExpeditionDefinition definition, int playerId)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            return new ExpeditionProgress
            {
                ExpeditionId = definition.Id,
                StableProgressKey = definition.GetStableProgressKey(playerId),
                StartGameTick = 0,
                RewardsClaimed = false,
                IsCompleted = false,
                IsActive = false,
                IsOrphaned = false,
            };
        }
    }
}

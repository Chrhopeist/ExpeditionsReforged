using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents the progress state of an expedition for a single player.
    /// Designed as a pure data model without direct dependencies on player logic.
    /// </summary>
    public sealed class ExpeditionProgress
    {
        /// <summary>
        /// Gets or sets the unique identifier for the expedition.
        /// </summary>
        public string ExpeditionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the game tick when the expedition was started.
        /// </summary>
        public long StartGameTick { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the expedition objectives are complete.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rewards for this expedition have been claimed.
        /// </summary>
        public bool RewardsClaimed { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the expedition is currently active (started but not completed).
        /// </summary>
        public bool IsActive => !IsCompleted;

        /// <summary>
        /// Marks the expedition as completed.
        /// </summary>
        public void Complete()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// Marks the expedition's rewards as claimed.
        /// </summary>
        public void ClaimRewards()
        {
            RewardsClaimed = true;
        }
    }
}

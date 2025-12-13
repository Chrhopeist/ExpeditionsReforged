using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents a single reward entry for an expedition. Supports both boolean unlocks and countable stacks.
    /// </summary>
    public sealed class RewardDefinition
    {
        /// <summary>
        /// Item ID or bespoke reward key. For in-game items this should map to <see cref="Terraria.ID.ItemID"/> or mod items.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Minimum quantity awarded when the reward triggers. A value of 1 with <see cref="MaxStack"/> 1 represents a boolean reward.
        /// </summary>
        public int MinStack { get; }

        /// <summary>
        /// Maximum quantity awarded when the reward triggers.
        /// </summary>
        public int MaxStack { get; }

        /// <summary>
        /// Optional drop chance between 0 and 1. Values below 1 indicate the reward is probabilistic.
        /// </summary>
        public float DropChance { get; }

        /// <summary>
        /// Indicates whether the reward is represented as a boolean unlock rather than a stack.
        /// </summary>
        public bool IsBoolean => MinStack == 1 && MaxStack == 1;

        public RewardDefinition(string id, int minStack = 1, int maxStack = 1, float dropChance = 1f)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Reward definitions require an identifier.", nameof(id));

            if (minStack < 1)
                minStack = 1;

            if (maxStack < minStack)
                maxStack = minStack;

            if (dropChance < 0f || dropChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(dropChance), "Drop chance must be between 0 and 1.");

            Id = id;
            MinStack = minStack;
            MaxStack = maxStack;
            DropChance = dropChance;
        }

        public RewardDefinition Clone() => new(Id, MinStack, MaxStack, DropChance);

        internal string SerializeForHash() => $"{Id}:{MinStack}:{MaxStack}:{DropChance}";
    }
}

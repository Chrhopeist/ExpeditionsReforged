using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents a single reward entry for an expedition, using raw <c>ItemID</c> values.
    /// </summary>
    public sealed class ExpeditionReward
    {
        /// <summary>
        /// The <c>ItemID</c> integer identifying the reward item.
        /// </summary>
        public int ItemId { get; }

        /// <summary>
        /// The minimum stack size awarded when the reward is granted.
        /// </summary>
        public int MinStack { get; }

        /// <summary>
        /// The maximum stack size awarded when the reward is granted.
        /// </summary>
        public int MaxStack { get; }

        /// <summary>
        /// The probability of the reward being granted, clamped between 0 and 1 inclusive.
        /// </summary>
        public float DropChance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpeditionReward"/> class with validation guards.
        /// </summary>
        /// <param name="itemId">The <c>ItemID</c> integer identifying the reward item.</param>
        /// <param name="minStack">The minimum stack size awarded.</param>
        /// <param name="maxStack">The maximum stack size awarded.</param>
        /// <param name="dropChance">The probability of the reward being granted.</param>
        public ExpeditionReward(int itemId, int minStack, int maxStack, float dropChance)
        {
            if (minStack < 1)
            {
                minStack = 1;
            }

            if (maxStack < minStack)
            {
                maxStack = minStack;
            }

            ItemId = itemId;
            MinStack = minStack;
            MaxStack = maxStack;
            DropChance = Math.Clamp(dropChance, 0f, 1f);
        }
    }
}

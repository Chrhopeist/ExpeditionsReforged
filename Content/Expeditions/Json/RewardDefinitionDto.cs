namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// JSON-facing representation of a reward entry for an expedition.
    /// </summary>
    public sealed class RewardDefinitionDto
    {
        /// <summary>
        /// Identifier for the reward, such as an item ID or custom key.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Minimum quantity awarded when the reward triggers.
        /// </summary>
        public int MinStack { get; set; }

        /// <summary>
        /// Maximum quantity awarded when the reward triggers.
        /// </summary>
        public int MaxStack { get; set; }

        /// <summary>
        /// Drop chance between 0 and 1 for probabilistic rewards.
        /// </summary>
        public float DropChance { get; set; }
    }
}

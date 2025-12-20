namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// JSON-facing representation of a prerequisite condition for an expedition.
    /// </summary>
    public sealed class ConditionDefinitionDto
    {
        /// <summary>
        /// Unique identifier for the condition.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Number of times the condition must be satisfied.
        /// </summary>
        public int RequiredCount { get; set; }

        /// <summary>
        /// Optional display text describing the condition.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}

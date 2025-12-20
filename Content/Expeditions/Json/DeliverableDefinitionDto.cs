namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// JSON-facing representation of a deliverable required by an expedition.
    /// </summary>
    public sealed class DeliverableDefinitionDto
    {
        /// <summary>
        /// Identifier for the deliverable, usually an item ID string.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Quantity required to satisfy the deliverable.
        /// </summary>
        public int RequiredCount { get; set; }

        /// <summary>
        /// Indicates whether the items should be consumed when delivered.
        /// </summary>
        public bool ConsumesItems { get; set; }

        /// <summary>
        /// Optional display text for UI prompts.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}

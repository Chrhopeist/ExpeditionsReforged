using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents an item or objective that must be delivered to complete an expedition.
    /// </summary>
    public sealed class DeliverableDefinition
    {
        /// <summary>
        /// Identifier for the deliverable; must be a string containing a numeric Terraria ItemID (for example, "23" for Gel).
        /// Symbolic values like "ItemID.Gel" are intentionally unsupported for now.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Quantity required to satisfy this deliverable. A value of 1 indicates a boolean requirement.
        /// </summary>
        public int RequiredCount { get; }

        /// <summary>
        /// Indicates whether the deliverable should be consumed when handed in.
        /// </summary>
        public bool ConsumesItems { get; }

        /// <summary>
        /// Optional display text for UI prompts.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// True when this deliverable behaves like a boolean switch rather than a stack counter.
        /// </summary>
        public bool IsBoolean => RequiredCount <= 1;

        public DeliverableDefinition(string id, int requiredCount, bool consumesItems, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Deliverable definitions require an identifier.", nameof(id));

            if (!int.TryParse(id, out int itemId) || itemId <= 0)
                throw new ArgumentException("Deliverable identifiers must be numeric Terraria ItemIDs stored as strings.", nameof(id));

            if (requiredCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requiredCount), "Deliverable counts must be positive.");

            // TODO: Allow non-numeric identifiers for mod items or custom objectives once loading is updated to support them safely.
            Id = id;
            RequiredCount = requiredCount;
            ConsumesItems = consumesItems;
            Description = description ?? string.Empty;
        }

        public DeliverableDefinition Clone() => new(Id, RequiredCount, ConsumesItems, Description);

        internal string SerializeForHash() => $"{Id}:{RequiredCount}:{ConsumesItems}:{Description}";
    }
}

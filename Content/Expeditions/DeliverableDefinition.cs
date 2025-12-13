using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents an item or objective that must be delivered to complete an expedition.
    /// </summary>
    public sealed class DeliverableDefinition
    {
        /// <summary>
        /// Identifier for the deliverable; typically an ItemID or bespoke objective key.
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

            if (requiredCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requiredCount), "Deliverable counts must be positive.");

            Id = id;
            RequiredCount = requiredCount;
            ConsumesItems = consumesItems;
            Description = description ?? string.Empty;
        }

        public DeliverableDefinition Clone() => new(Id, RequiredCount, ConsumesItems, Description);

        internal string SerializeForHash() => $"{Id}:{RequiredCount}:{ConsumesItems}:{Description}";
    }
}

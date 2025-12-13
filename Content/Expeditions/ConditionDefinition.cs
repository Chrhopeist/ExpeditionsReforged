using System;

namespace ExpeditionsReforged.Content.Expeditions
{
    /// <summary>
    /// Represents a prerequisite that must be satisfied before an expedition can be accepted.
    /// Conditions can be simple boolean flags (e.g., boss defeated) or numeric thresholds.
    /// </summary>
    public sealed class ConditionDefinition
    {
        /// <summary>
        /// Unique identifier for the condition.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The number of times the condition must be satisfied. Defaults to 1 for boolean flags.
        /// </summary>
        public int RequiredCount { get; }

        /// <summary>
        /// Optional text describing the condition for UI purposes.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Indicates whether this condition represents a simple boolean flag.
        /// </summary>
        public bool IsBoolean => RequiredCount <= 1;

        public ConditionDefinition(string id, int requiredCount = 1, string description = "")
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Condition definitions require an identifier.", nameof(id));

            if (requiredCount < 1)
                throw new ArgumentOutOfRangeException(nameof(requiredCount), "Condition counts must be positive.");

            Id = id;
            RequiredCount = requiredCount;
            Description = description ?? string.Empty;
        }

        public ConditionDefinition Clone() => new(Id, RequiredCount, Description);

        internal string SerializeForHash() => $"{Id}:{RequiredCount}:{Description}";
    }
}

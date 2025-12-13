using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ExpeditionsReforged.Content.Expeditions;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Centralized registry for expedition definitions. Provides validated, multiplayer-safe access to
    /// all available expeditions so gameplay and UI systems can query a single authoritative source.
    /// </summary>
    public class ExpeditionRegistry : ModSystem
    {
        private ImmutableDictionary<string, ExpeditionDefinition> _definitions =
            ImmutableDictionary<string, ExpeditionDefinition>.Empty.WithComparers(StringComparer.Ordinal);

        /// <summary>
        /// Read-only view of all registered expedition definitions keyed by their unique IDs.
        /// </summary>
        public IReadOnlyDictionary<string, ExpeditionDefinition> Definitions => _definitions;

        public override void Load()
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ExpeditionDefinition>(StringComparer.Ordinal);

            RegisterExpedition(builder, new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayName: "Forest Scout",
                durationTicks: 60 * 60 * 8,
                difficulty: 1,
                minPlayerLevel: 1,
                isRepeatable: true));

            RegisterExpedition(builder, new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayName: "Desert Run",
                durationTicks: 60 * 60 * 12,
                difficulty: 2,
                minPlayerLevel: 3,
                isRepeatable: true));

            RegisterExpedition(builder, new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayName: "Dungeon Probe",
                durationTicks: 60 * 60 * 24,
                difficulty: 4,
                minPlayerLevel: 6,
                isRepeatable: false));

            _definitions = builder.ToImmutable();
        }

        public override void Unload()
        {
            _definitions = ImmutableDictionary<string, ExpeditionDefinition>.Empty.WithComparers(StringComparer.Ordinal);
        }

        /// <summary>
        /// Retrieves a read-only collection of all registered expedition definitions.
        /// </summary>
        public IReadOnlyCollection<ExpeditionDefinition> GetAll() => _definitions.Values;

        /// <summary>
        /// Attempts to retrieve a registered expedition definition by its ID.
        /// </summary>
        /// <param name="id">Expedition identifier to look up.</param>
        /// <param name="definition">Matching definition if found.</param>
        /// <returns>True if the definition exists; otherwise false.</returns>
        public bool TryGetById(string id, out ExpeditionDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(id, out definition);
        }

        private static void RegisterExpedition(
            ImmutableDictionary<string, ExpeditionDefinition>.Builder builder,
            ExpeditionDefinition definition)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ArgumentException("Expedition definitions must supply a non-empty ID.", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
                throw new ArgumentException("Expedition definitions must supply a display name.", nameof(definition));

            if (definition.DurationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Expedition duration must be greater than zero.");

            if (definition.Difficulty <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Expedition difficulty must be positive.");

            if (definition.MinPlayerLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Minimum player level cannot be negative.");

            if (builder.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate expedition id '{definition.Id}' detected during registration.");

            builder.Add(definition.Id, definition);
        }
    }
}

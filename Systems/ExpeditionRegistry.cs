using System;
using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<string, ExpeditionDefinition> _definitions = new(StringComparer.Ordinal);
        private IReadOnlyCollection<ExpeditionDefinition> _definitionCollection = Array.Empty<ExpeditionDefinition>();

        /// <summary>
        /// Read-only view of all registered expedition definitions.
        /// </summary>
        public IReadOnlyCollection<ExpeditionDefinition> Definitions => _definitionCollection;

        public override void Load()
        {
            var definitions = new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal);

            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayName: "Forest Scout",
                durationTicks: 60 * 60 * 8,
                difficulty: 1,
                minPlayerLevel: 1,
                isRepeatable: true));

            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayName: "Desert Run",
                durationTicks: 60 * 60 * 12,
                difficulty: 2,
                minPlayerLevel: 3,
                isRepeatable: true));

            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayName: "Dungeon Probe",
                durationTicks: 60 * 60 * 24,
                difficulty: 4,
                minPlayerLevel: 6,
                isRepeatable: false));

            _definitions = definitions;
            _definitionCollection = definitions.Values.ToList();
        }

        public override void Unload()
        {
            _definitions = new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal);
            _definitionCollection = Array.Empty<ExpeditionDefinition>();
        }

        /// <summary>
        /// Retrieves a read-only collection of all registered expedition definitions.
        /// </summary>
        public IReadOnlyCollection<ExpeditionDefinition> GetAll() => _definitionCollection;

        /// <summary>
        /// Attempts to retrieve a registered expedition definition by its ID.
        /// </summary>
        /// <param name="expeditionId">Expedition identifier to look up.</param>
        /// <param name="definition">Matching definition if found.</param>
        /// <returns>True if the definition exists; otherwise false.</returns>
        public bool TryGetDefinition(string expeditionId, out ExpeditionDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(expeditionId, out definition);
        }

        private static void RegisterExpedition(
            Dictionary<string, ExpeditionDefinition> definitions,
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

            if (definitions.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate expedition id '{definition.Id}' detected during registration.");

            definitions.Add(definition.Id, definition);
        }
    }
}

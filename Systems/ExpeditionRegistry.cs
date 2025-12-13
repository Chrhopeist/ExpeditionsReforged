using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, ExpeditionDefinition> _definitions = new(StringComparer.Ordinal);

        /// <summary>
        /// Read-only view of all registered expedition definitions keyed by their unique IDs.
        /// </summary>
        public IReadOnlyDictionary<string, ExpeditionDefinition> Definitions => _definitions;

        public override void Load()
        {
            _definitions.Clear();

            RegisterExpedition(new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayName: "Forest Scout",
                durationTicks: 60 * 60 * 8,
                difficulty: 1,
                minPlayerLevel: 1,
                isRepeatable: true));

            RegisterExpedition(new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayName: "Desert Run",
                durationTicks: 60 * 60 * 12,
                difficulty: 2,
                minPlayerLevel: 3,
                isRepeatable: true));

            RegisterExpedition(new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayName: "Dungeon Probe",
                durationTicks: 60 * 60 * 24,
                difficulty: 4,
                minPlayerLevel: 6,
                isRepeatable: false));
        }

        public override void Unload()
        {
            _definitions.Clear();
        }

        /// <summary>
        /// Attempts to retrieve a registered expedition definition by its ID.
        /// </summary>
        /// <param name="id">Expedition identifier to look up.</param>
        /// <param name="definition">Matching definition if found.</param>
        /// <returns>True if the definition exists; otherwise false.</returns>
        public bool TryGetDefinition(string id, out ExpeditionDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(id, out definition);
        }

        private void RegisterExpedition(ExpeditionDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ArgumentException("Expedition definitions must supply a non-empty ID.", nameof(definition));

            if (_definitions.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate expedition id '{definition.Id}' detected during registration.");

            _definitions.Add(definition.Id, definition);
        }
    }
}

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

            RegisterInternalExpeditions(definitions);

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

        /// <summary>
        /// Registers an expedition definition, performing validation and cloning to ensure immutability.
        /// Safe for mod authors to call during loading to extend the registry.
        /// </summary>
        public void RegisterExpedition(ExpeditionDefinition definition)
        {
            RegisterExpedition(_definitions, definition);
            _definitionCollection = _definitions.Values.ToList();
        }

        /// <summary>
        /// Returns expeditions that belong to the given category (case-insensitive).
        /// </summary>
        public IEnumerable<ExpeditionDefinition> GetByCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return Enumerable.Empty<ExpeditionDefinition>();

            return _definitionCollection.Where(d => string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns expeditions that satisfy a predicate against player progress state.
        /// </summary>
        public IEnumerable<ExpeditionDefinition> FilterByProgress(IEnumerable<ExpeditionProgress> progressStates, Func<ExpeditionDefinition, ExpeditionProgress?, bool> predicate)
        {
            if (progressStates is null)
                throw new ArgumentNullException(nameof(progressStates));

            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            var progressLookup = progressStates.ToDictionary(p => p.ExpeditionId, StringComparer.OrdinalIgnoreCase);
            foreach (var definition in _definitionCollection)
            {
                progressLookup.TryGetValue(definition.Id, out var progress);
                if (predicate(definition, progress))
                {
                    yield return definition;
                }
            }
        }

        /// <summary>
        /// Retrieves expeditions that are eligible for daily selection.
        /// </summary>
        public IEnumerable<ExpeditionDefinition> GetDailyEligible() => _definitionCollection.Where(d => d.IsDailyEligible);

        /// <summary>
        /// Provides a cloned instance of the expedition with a stable progress hash for a specific player.
        /// </summary>
        public ExpeditionDefinition CloneForPlayer(string expeditionId, int playerId)
        {
            if (!TryGetDefinition(expeditionId, out var definition))
                throw new KeyNotFoundException($"Unknown expedition id '{expeditionId}'.");

            var clone = definition.Clone();
            _ = clone.GetStableProgressKey(playerId); // ensures the hash is calculated to keep consistency.
            return clone;
        }

        private static void RegisterInternalExpeditions(Dictionary<string, ExpeditionDefinition> definitions)
        {
            // Expedition names, descriptions, and categories are provided as localization keys so the registry remains
            // language-neutral and UI layers can resolve player-facing strings via Language.GetTextValue at render time.
            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.ForestScout.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.ForestScout.Description",
                categoryKey: "Mods.ExpeditionsReforged.ExpeditionCategories.Forest",
                rarity: 1,
                durationTicks: 60 * 60 * 8,
                difficulty: 1,
                minPlayerLevel: 1,
                isRepeatable: true,
                isDailyEligible: true,
                npcHeadId: 1,
                prerequisites: new[] { new ConditionDefinition("boss:eye_of_cthulhu", 1, "Defeat the Eye of Cthulhu") },
                deliverables: new[] { new DeliverableDefinition("item:wood", 30, true, "Deliver Wood") },
                rewards: new[] { new RewardDefinition("ItemID.CopperCoin", 25, 50) },
                dailyRewards: new[] { new RewardDefinition("ItemID.SilverCoin", 5, 15) }));

            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.DesertRun.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.DesertRun.Description",
                categoryKey: "Mods.ExpeditionsReforged.ExpeditionCategories.Desert",
                rarity: 2,
                durationTicks: 60 * 60 * 12,
                difficulty: 2,
                minPlayerLevel: 3,
                isRepeatable: true,
                isDailyEligible: true,
                npcHeadId: 2,
                prerequisites: new[] { new ConditionDefinition("unlock:desert", 1, "Discover the desert") },
                deliverables: new[] { new DeliverableDefinition("item:cactus", 15, true, "Gather Cactus") },
                rewards: new[] { new RewardDefinition("ItemID.GoldCoin", 1, 2) },
                dailyRewards: new[] { new RewardDefinition("ItemID.SilverCoin", 10, 25) }));

            RegisterExpedition(definitions, new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.DungeonProbe.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.DungeonProbe.Description",
                categoryKey: "Mods.ExpeditionsReforged.ExpeditionCategories.Dungeon",
                rarity: 3,
                durationTicks: 60 * 60 * 24,
                difficulty: 4,
                minPlayerLevel: 6,
                isRepeatable: false,
                isDailyEligible: false,
                npcHeadId: 3,
                prerequisites: new[] { new ConditionDefinition("boss:skeletron", 1, "Defeat Skeletron") },
                deliverables: new[] { new DeliverableDefinition("item:bone", 20, true, "Collect Bones") },
                rewards: new[] { new RewardDefinition("ItemID.GoldCoin", 5, 5), new RewardDefinition("ItemID.ShadowKey", 1, 1, 0.5f) }));
        }

        private static void RegisterExpedition(
            Dictionary<string, ExpeditionDefinition> definitions,
            ExpeditionDefinition definition)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new ArgumentException("Expedition definitions must supply a non-empty ID.", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.DisplayNameKey))
                throw new ArgumentException("Expedition definitions must supply a display name localization key.", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.DescriptionKey))
                throw new ArgumentException("Expedition definitions must supply a description localization key.", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.CategoryKey))
                throw new ArgumentException("Expedition definitions must supply a category localization key.", nameof(definition));

            if (definition.DurationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Expedition duration must be greater than zero.");

            if (definition.Difficulty <= 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Expedition difficulty must be positive.");

            if (definition.MinPlayerLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Minimum player level cannot be negative.");

            if (definition.Rarity < 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Rarity cannot be negative.");

            ValidateCollections(definition);

            if (definitions.ContainsKey(definition.Id))
                throw new InvalidOperationException($"Duplicate expedition id '{definition.Id}' detected during registration.");

            definitions.Add(definition.Id, definition.Clone());
        }

        private static void ValidateCollections(ExpeditionDefinition definition)
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                _ = prerequisite ?? throw new ArgumentException("Null prerequisite definition detected.", nameof(definition));
            }

            foreach (var deliverable in definition.Deliverables)
            {
                _ = deliverable ?? throw new ArgumentException("Null deliverable definition detected.", nameof(definition));
            }

            foreach (var reward in definition.Rewards)
            {
                _ = reward ?? throw new ArgumentException("Null reward definition detected.", nameof(definition));
            }

            foreach (var reward in definition.DailyRewards)
            {
                _ = reward ?? throw new ArgumentException("Null daily reward definition detected.", nameof(definition));
            }
        }
    }
}

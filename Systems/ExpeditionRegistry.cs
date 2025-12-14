using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Centralized registry for expedition definitions. Provides validated, multiplayer-safe access to
    /// all available expeditions so gameplay and UI systems can query a single authoritative source.
    /// </summary>
    public class ExpeditionRegistry : ModSystem
    {
        private IReadOnlyDictionary<string, ExpeditionDefinition> _definitions = new ReadOnlyDictionary<string, ExpeditionDefinition>(new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal));
        private IReadOnlyCollection<ExpeditionDefinition> _definitionCollection = Array.Empty<ExpeditionDefinition>();

        /// <summary>
        /// Read-only view of all registered expedition definitions.
        /// </summary>
        public IReadOnlyCollection<ExpeditionDefinition> Definitions => _definitionCollection;

        public override void Load()
        {
            var definitions = new List<ExpeditionDefinition>();
            RegisterInternalExpeditions(definitions);

            FinalizeDefinitions(definitions);
        }

        public override void Unload()
        {
            _definitions = new ReadOnlyDictionary<string, ExpeditionDefinition>(new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal));
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
        public bool TryGetExpedition(string expeditionId, out ExpeditionDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(expeditionId, out definition);
        }

        /// <summary>
        /// Obsolete shim for existing callers. Prefer <see cref="TryGetExpedition"/>.
        /// </summary>
        public bool TryGetDefinition(string expeditionId, out ExpeditionDefinition definition) => TryGetExpedition(expeditionId, out definition);

        /// <summary>
        /// Returns expeditions that belong to the given category.
        /// </summary>
        public IEnumerable<ExpeditionDefinition> GetByCategory(ExpeditionCategory category)
        {
            if (!Enum.IsDefined(typeof(ExpeditionCategory), category) || category == ExpeditionCategory.Unknown)
                return Enumerable.Empty<ExpeditionDefinition>();

            return _definitionCollection.Where(d => d.Category == category);
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
            if (!TryGetExpedition(expeditionId, out var definition))
                throw new KeyNotFoundException($"Unknown expedition id '{expeditionId}'.");

            var clone = definition.Clone();
            _ = clone.GetStableProgressKey(playerId); // ensures the hash is calculated to keep consistency.
            return clone;
        }

        private void RegisterInternalExpeditions(ICollection<ExpeditionDefinition> definitions)
        {
            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayName: "Forest Scout",
                category: ExpeditionCategory.Forest,
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

            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayName: "Desert Run",
                category: ExpeditionCategory.Desert,
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

            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayName: "Dungeon Probe",
                category: ExpeditionCategory.Dungeon,
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

        private void FinalizeDefinitions(IEnumerable<ExpeditionDefinition> definitions)
        {
            var validated = new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (TryValidateDefinition(definition, validated, out ExpeditionDefinition validatedDefinition))
                {
                    validated.Add(validatedDefinition.Id, validatedDefinition);
                }
            }

            _definitions = new ReadOnlyDictionary<string, ExpeditionDefinition>(validated);
            _definitionCollection = Array.AsReadOnly(validated.Values.ToArray());

            Logger.Info($"Registered {_definitionCollection.Count} expeditions.");
        }

        private bool TryValidateDefinition(ExpeditionDefinition definition, IDictionary<string, ExpeditionDefinition> existing, out ExpeditionDefinition validated)
        {
            validated = null;

            if (definition is null)
            {
                Logger.Warn("Skipping null expedition definition.");
                return false;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Logger.Warn("Expedition definitions must supply a non-empty ID. Definition skipped.");
                    return false;
                }

                if (existing.ContainsKey(definition.Id))
                {
                    Logger.Warn($"Duplicate expedition id '{definition.Id}' detected during registration. The duplicate was ignored.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    Logger.Warn($"Expedition '{definition.Id}' has no display name and was skipped.");
                    return false;
                }

                if (!Enum.IsDefined(typeof(ExpeditionCategory), definition.Category) || definition.Category == ExpeditionCategory.Unknown)
                {
                    Logger.Warn($"Expedition '{definition.Id}' uses an undefined category '{definition.Category}'. The definition was skipped.");
                    return false;
                }

                if (definition.NpcHeadId >= 0 && (definition.NpcHeadId >= TextureAssets.NpcHead.Length))
                {
                    Logger.Warn($"Expedition '{definition.Id}' references invalid NPC head id {definition.NpcHeadId}. The definition was skipped.");
                    return false;
                }

                if (definition.DurationTicks <= 0)
                {
                    Logger.Warn($"Expedition '{definition.Id}' has non-positive duration and was skipped.");
                    return false;
                }

                if (definition.Difficulty <= 0)
                {
                    Logger.Warn($"Expedition '{definition.Id}' has non-positive difficulty and was skipped.");
                    return false;
                }

                if (definition.MinPlayerLevel < 0)
                {
                    Logger.Warn($"Expedition '{definition.Id}' has a negative minimum player level and was skipped.");
                    return false;
                }

                if (definition.Rarity < 0)
                {
                    Logger.Warn($"Expedition '{definition.Id}' has a negative rarity and was skipped.");
                    return false;
                }

                if (!ValidateCollections(definition))
                {
                    return false;
                }

                validated = definition.Clone();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to register expedition '{definition?.Id ?? "<unknown>"}': {ex.Message}");
                return false;
            }
        }

        private bool ValidateCollections(ExpeditionDefinition definition)
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                if (prerequisite is null)
                {
                    Logger.Warn($"Expedition '{definition.Id}' contains a null prerequisite and was skipped.");
                    return false;
                }
            }

            foreach (var deliverable in definition.Deliverables)
            {
                if (deliverable is null)
                {
                    Logger.Warn($"Expedition '{definition.Id}' contains a null deliverable and was skipped.");
                    return false;
                }
            }

            foreach (var reward in definition.Rewards)
            {
                if (reward is null)
                {
                    Logger.Warn($"Expedition '{definition.Id}' contains a null reward and was skipped.");
                    return false;
                }
            }

            foreach (var reward in definition.DailyRewards)
            {
                if (reward is null)
                {
                    Logger.Warn($"Expedition '{definition.Id}' contains a null daily reward and was skipped.");
                    return false;
                }
            }

            return true;
        }
    }
}

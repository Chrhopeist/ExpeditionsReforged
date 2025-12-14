using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Centralized registry for expedition definitions. Provides validated, multiplayer-safe access to
    /// all available expeditions so gameplay and UI systems can query a single authoritative source.
    /// </summary>
    public class ExpeditionRegistry : ModSystem
    {
        private IReadOnlyDictionary<string, ExpeditionDefinition> _definitions =
            new ReadOnlyDictionary<string, ExpeditionDefinition>(new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal));

        private IReadOnlyCollection<ExpeditionDefinition> _definitionCollection = Array.Empty<ExpeditionDefinition>();

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

        public IReadOnlyCollection<ExpeditionDefinition> GetAll() => _definitionCollection;

        public bool TryGetExpedition(string expeditionId, out ExpeditionDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                definition = null;
                return false;
            }

            return _definitions.TryGetValue(expeditionId, out definition);
        }

        public bool TryGetDefinition(string expeditionId, out ExpeditionDefinition definition) => TryGetExpedition(expeditionId, out definition);

        public IEnumerable<ExpeditionDefinition> GetByCategory(ExpeditionCategory category)
        {
            if (!Enum.IsDefined(typeof(ExpeditionCategory), category) || category == ExpeditionCategory.Unknown)
                return Enumerable.Empty<ExpeditionDefinition>();

            return _definitionCollection.Where(d => d.Category == category);
        }

        public IEnumerable<ExpeditionDefinition> FilterByProgress(
            IEnumerable<ExpeditionProgress> progressStates,
            Func<ExpeditionDefinition, ExpeditionProgress?, bool> predicate)
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
                    yield return definition;
            }
        }

        public IEnumerable<ExpeditionDefinition> GetDailyEligible() => _definitionCollection.Where(d => d.IsDailyEligible);

        public ExpeditionDefinition CloneForPlayer(string expeditionId, int playerId)
        {
            if (!TryGetExpedition(expeditionId, out var definition))
                throw new KeyNotFoundException($"Unknown expedition id '{expeditionId}'.");

            var clone = definition.Clone();
            _ = clone.GetStableProgressKey(playerId);
            return clone;
        }

        private static string ItemCondition(int itemType) => $"item:{itemType}";
        private static string NpcCondition(int npcType) => $"npc:{npcType}";
        private static string RewardItem(int itemType) => $"item:{itemType}";

        private void RegisterInternalExpeditions(ICollection<ExpeditionDefinition> definitions)
        {
            // FIRST FULLY PLAYABLE EXPEDITION:
            // - Track kill + collect via your existing GlobalNPC/GlobalItem reporters.
            // - Auto-completes when all deliverables meet their required counts (already in ApplyConditionProgress).
            // - Rewards are paid on server when claimed.

            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:forest_scout",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.ForestScout.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.ForestScout.Description",
                category: ExpeditionCategory.Forest,
                rarity: 1,
                durationTicks: 60 * 60 * 8,
                difficulty: 1,
                minPlayerLevel: 1,
                isRepeatable: true,
                isDailyEligible: true,
                npcHeadId: 1,

                // Prerequisites are currently not enforced by gameplay logic (service returns true).
                // Keep empty for now so it is always startable.
                prerequisites: Array.Empty<ConditionDefinition>(),

                // Deliverables are the *tracked* conditions.
                // These IDs MUST match what your reporters emit:
                //   - ReportKill -> "npc:{npc.type}"
                //   - ReportItemPickup -> "item:{item.type}"
                deliverables: new[]
                {
                    new DeliverableDefinition(
                        id: NpcCondition(NPCID.Zombie),
                        requiredCount: 5,
                        consumesItems: false,
                        description: "Slay Zombies"),

                    new DeliverableDefinition(
                        id: ItemCondition(ItemID.Wood),
                        requiredCount: 25,
                        consumesItems: false,
                        description: "Gather Wood")
                },

                // Rewards are paid when claimed (server-authoritative).
                rewards: new[]
                {
                    new RewardDefinition(
                        id: RewardItem(ItemID.SilverCoin),
                        minStack: 15,
                        maxStack: 30,
                        dropChance: 1f)
                },

                dailyRewards: Array.Empty<RewardDefinition>()
            ));

            // Optional: keep other definitions registered, but make them consistent so they don't
            // silently fail tracking if a tester starts them.
            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:desert_run",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.DesertRun.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.DesertRun.Description",
                category: ExpeditionCategory.Desert,
                rarity: 2,
                durationTicks: 60 * 60 * 12,
                difficulty: 2,
                minPlayerLevel: 3,
                isRepeatable: true,
                isDailyEligible: true,
                npcHeadId: 2,
                prerequisites: Array.Empty<ConditionDefinition>(),
                deliverables: new[] { new DeliverableDefinition(ItemCondition(ItemID.Cactus), 15, false, "Gather Cactus") },
                rewards: new[] { new RewardDefinition(RewardItem(ItemID.GoldCoin), 1, 1) },
                dailyRewards: Array.Empty<RewardDefinition>()));

            definitions.Add(new ExpeditionDefinition(
                id: "expeditions:dungeon_probe",
                displayNameKey: "Mods.ExpeditionsReforged.Expeditions.DungeonProbe.DisplayName",
                descriptionKey: "Mods.ExpeditionsReforged.Expeditions.DungeonProbe.Description",
                category: ExpeditionCategory.Dungeon,
                rarity: 3,
                durationTicks: 60 * 60 * 24,
                difficulty: 4,
                minPlayerLevel: 6,
                isRepeatable: false,
                isDailyEligible: false,
                npcHeadId: 3,
                prerequisites: Array.Empty<ConditionDefinition>(),
                deliverables: new[] { new DeliverableDefinition(ItemCondition(ItemID.Bone), 20, false, "Collect Bones") },
                rewards: new[]
                {
                    new RewardDefinition(RewardItem(ItemID.GoldCoin), 5, 5),
                    new RewardDefinition(RewardItem(ItemID.ShadowKey), 1, 1, 0.5f)
                },
                dailyRewards: Array.Empty<RewardDefinition>()));
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

            Mod.Logger.Info($"Registered {_definitionCollection.Count} expeditions.");
        }

        private bool TryValidateDefinition(ExpeditionDefinition definition, IDictionary<string, ExpeditionDefinition> existing, out ExpeditionDefinition validated)
        {
            validated = null;

            if (definition is null)
            {
                Mod.Logger.Warn("Skipping null expedition definition.");
                return false;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Mod.Logger.Warn("Expedition definitions must supply a non-empty ID. Definition skipped.");
                    return false;
                }

                if (existing.ContainsKey(definition.Id))
                {
                    Mod.Logger.Warn($"Duplicate expedition id '{definition.Id}' detected during registration. The duplicate was ignored.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' has no display name and was skipped.");
                    return false;
                }

                if (!Enum.IsDefined(typeof(ExpeditionCategory), definition.Category) || definition.Category == ExpeditionCategory.Unknown)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' uses an undefined category '{definition.Category}'. The definition was skipped.");
                    return false;
                }

                if (definition.NpcHeadId >= 0 && (definition.NpcHeadId >= TextureAssets.NpcHead.Length))
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' references invalid NPC head id {definition.NpcHeadId}. The definition was skipped.");
                    return false;
                }

                if (definition.DurationTicks <= 0)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' has non-positive duration and was skipped.");
                    return false;
                }

                if (definition.Difficulty <= 0)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' has non-positive difficulty and was skipped.");
                    return false;
                }

                if (definition.MinPlayerLevel < 0)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' has a negative minimum player level and was skipped.");
                    return false;
                }

                if (definition.Rarity < 0)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' has a negative rarity and was skipped.");
                    return false;
                }

                if (!ValidateCollections(definition))
                    return false;

                validated = definition.Clone();
                return true;
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn($"Failed to register expedition '{definition?.Id ?? "<unknown>"}': {ex.Message}");
                return false;
            }
        }

        private bool ValidateCollections(ExpeditionDefinition definition)
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                if (prerequisite is null)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' contains a null prerequisite and was skipped.");
                    return false;
                }
            }

            foreach (var deliverable in definition.Deliverables)
            {
                if (deliverable is null)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' contains a null deliverable and was skipped.");
                    return false;
                }
            }

            foreach (var reward in definition.Rewards)
            {
                if (reward is null)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' contains a null reward and was skipped.");
                    return false;
                }
            }

            foreach (var reward in definition.DailyRewards)
            {
                if (reward is null)
                {
                    Mod.Logger.Warn($"Expedition '{definition.Id}' contains a null daily reward and was skipped.");
                    return false;
                }
            }

            return true;
        }
    }
}

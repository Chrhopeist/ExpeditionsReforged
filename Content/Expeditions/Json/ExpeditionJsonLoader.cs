using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// Loads expedition definitions from expeditions.json stored in the mod save folder.
    /// </summary>
    public static class ExpeditionJsonLoader
    {
        private const string ExpeditionsFileName = "expeditions.json";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Loads expedition definitions from the mod save folder.
        /// This method must not be invoked on multiplayer clients.
        /// </summary>
        /// <returns>Read-only list of expedition definitions.</returns>
        public static IReadOnlyList<ExpeditionDefinition> LoadExpeditions()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                throw new InvalidOperationException("Expedition JSON loading is server-authoritative and cannot run on multiplayer clients.");
            }

            Mod mod = ModContent.GetInstance<ExpeditionsReforged>();
            string saveFolder = Path.Combine(Main.SavePath, "ModLoader", mod.Name);
            Directory.CreateDirectory(saveFolder);
            string filePath = Path.Combine(saveFolder, ExpeditionsFileName);

            if (!File.Exists(filePath))
            {
                ThrowLoggedError(mod, $"Expedition JSON file not found at '{filePath}'.", new FileNotFoundException("Missing expeditions.json", filePath));
            }

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                ThrowLoggedError(mod, $"Failed to read expedition JSON from '{filePath}'.", ex);
            }

            List<ExpeditionDefinitionDto> dtos;
            try
            {
                dtos = JsonSerializer.Deserialize<List<ExpeditionDefinitionDto>>(json, SerializerOptions)
                    ?? throw new InvalidDataException("Expedition JSON deserialized to null.");
            }
            catch (JsonException ex)
            {
                ThrowLoggedError(mod, $"Invalid expedition JSON in '{filePath}'.", new InvalidDataException("Expedition JSON could not be parsed.", ex));
            }
            catch (Exception ex)
            {
                ThrowLoggedError(mod, $"Unexpected error while parsing expedition JSON in '{filePath}'.", ex);
            }

            var definitions = new List<ExpeditionDefinition>(dtos.Count);
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < dtos.Count; index++)
            {
                ExpeditionDefinitionDto dto = dtos[index] ?? throw new InvalidDataException($"Expedition entry at index {index} is null.");

                if (string.IsNullOrWhiteSpace(dto.Id))
                {
                    ThrowLoggedError(mod, $"Expedition entry at index {index} has an empty Id.", new InvalidDataException("Expedition Ids must be non-empty."));
                }

                if (!seenIds.Add(dto.Id))
                {
                    ThrowLoggedError(mod, $"Duplicate expedition Id '{dto.Id}' detected.", new InvalidDataException("Expedition Ids must be unique."));
                }

                try
                {
                    ExpeditionCategory category = ParseCategory(dto.Category, dto.Id, mod);
                    int minPlayerLevel = ParseProgressionTier(dto.MinProgressionTier, dto.Id, mod);

                    var prerequisites = (dto.Prerequisites ?? new List<ConditionDefinitionDto>())
                        .Select(condition => new ConditionDefinition(condition.Id, condition.RequiredCount, condition.Description))
                        .ToList();

                    var deliverables = (dto.Deliverables ?? new List<DeliverableDefinitionDto>())
                        .Select(deliverable => new DeliverableDefinition(deliverable.Id, deliverable.RequiredCount, deliverable.ConsumesItems, deliverable.Description))
                        .ToList();

                    var rewards = (dto.Rewards ?? new List<RewardDefinitionDto>())
                        .Select(reward => new RewardDefinition(reward.Id, reward.MinStack, reward.MaxStack, reward.DropChance))
                        .ToList();

                    var dailyRewards = (dto.DailyRewards ?? new List<RewardDefinitionDto>())
                        .Select(reward => new RewardDefinition(reward.Id, reward.MinStack, reward.MaxStack, reward.DropChance))
                        .ToList();

                    definitions.Add(new ExpeditionDefinition(
                        id: dto.Id,
                        displayNameKey: dto.DisplayNameKey,
                        descriptionKey: dto.DescriptionKey,
                        category: category,
                        rarity: dto.Rarity,
                        durationTicks: dto.DurationTicks,
                        difficulty: dto.Difficulty,
                        minPlayerLevel: minPlayerLevel,
                        isRepeatable: dto.IsRepeatable,
                        isDailyEligible: dto.IsDailyEligible,
                        npcHeadId: dto.NpcHeadId,
                        prerequisites: prerequisites,
                        deliverables: deliverables,
                        rewards: rewards,
                        dailyRewards: dailyRewards));
                }
                catch (Exception ex)
                {
                    ThrowLoggedError(mod, $"Failed to build expedition definition '{dto.Id}'.", ex);
                }
            }

            return definitions.AsReadOnly();
        }

        private static ExpeditionCategory ParseCategory(string categoryValue, string expeditionId, Mod mod)
        {
            if (!Enum.TryParse(categoryValue, true, out ExpeditionCategory category) ||
                !Enum.IsDefined(typeof(ExpeditionCategory), category) ||
                category == ExpeditionCategory.Unknown)
            {
                ThrowLoggedError(mod, $"Expedition '{expeditionId}' uses invalid category '{categoryValue}'.", new InvalidDataException("Invalid expedition category."));
            }

            return category;
        }

        private static int ParseProgressionTier(string tierValue, string expeditionId, Mod mod)
        {
            // Progression tiers are represented as positive integer strings until a dedicated progression system is implemented.
            string trimmed = tierValue?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmed))
            {
                return 1;
            }

            if (!int.TryParse(trimmed, out int minPlayerLevel) || minPlayerLevel < 1)
            {
                ThrowLoggedError(mod, $"Expedition '{expeditionId}' uses invalid progression tier '{tierValue}'.", new InvalidDataException("Progression tiers must be positive integers represented as strings."));
            }

            return minPlayerLevel;
        }

        [DoesNotReturn]
        private static void ThrowLoggedError(Mod mod, string message, Exception exception)
        {
            mod.Logger.Error(message, exception);
            throw exception;
        }
    }
}

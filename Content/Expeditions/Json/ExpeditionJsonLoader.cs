using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        private const string EmbeddedExpeditionsAssetPath = "Content/Expeditions/expeditions.json";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Loads expedition DTOs from the mod save folder.
        /// This method must not be invoked on multiplayer clients.
        /// </summary>
        /// <returns>Read-only list of expedition DTOs.</returns>
        public static IReadOnlyList<ExpeditionDefinitionDto> LoadExpeditionDtos()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                throw new InvalidOperationException("Expedition JSON loading is server-authoritative and cannot run on multiplayer clients.");
            }

            Mod mod = ModContent.GetInstance<ExpeditionsReforged>();
            string filePath = GetExpeditionJsonPath(mod);

            if (!File.Exists(filePath))
            {
                // Server and single-player sessions seed the save folder from the embedded asset.
                // Multiplayer clients must never access embedded expedition data.
                if (!TryEnsureDefaultJsonExists(mod, filePath))
                {
                    // Missing file should not hard-fail mod compilation; log and continue with no expeditions.
                    mod.Logger.Error($"Expedition JSON file not found at '{filePath}'.");
                    return Array.Empty<ExpeditionDefinitionDto>();
                }
            }

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                // File IO can fail due to OS locks or permissions; log and continue with no expeditions.
                mod.Logger.Error($"Failed to read expedition JSON from '{filePath}'.", ex);
                return Array.Empty<ExpeditionDefinitionDto>();
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                // Empty payload is treated as invalid and yields no expeditions.
                mod.Logger.Error($"Expedition JSON file '{filePath}' was empty.");
                return Array.Empty<ExpeditionDefinitionDto>();
            }

            return DeserializeExpeditionDtos(json, filePath, mod);
        }

        /// <summary>
        /// Serializes expedition DTOs to a JSON blob suitable for sync.
        /// </summary>
        public static string SerializeExpeditionDtos(IReadOnlyList<ExpeditionDefinitionDto> dtos)
        {
            if (dtos is null)
            {
                throw new ArgumentNullException(nameof(dtos));
            }

            return JsonSerializer.Serialize(dtos, SerializerOptions);
        }

        /// <summary>
        /// Deserializes expedition DTOs from a JSON payload.
        /// </summary>
        public static IReadOnlyList<ExpeditionDefinitionDto> DeserializeExpeditionDtos(string json)
        {
            Mod mod = ModContent.GetInstance<ExpeditionsReforged>();
            return DeserializeExpeditionDtos(json, "expedition sync payload", mod);
        }

        /// <summary>
        /// Builds expedition definitions from DTOs.
        /// </summary>
        public static IReadOnlyList<ExpeditionDefinition> BuildDefinitions(IReadOnlyList<ExpeditionDefinitionDto> dtos)
        {
            if (dtos is null)
            {
                throw new ArgumentNullException(nameof(dtos));
            }

            Mod mod = ModContent.GetInstance<ExpeditionsReforged>();
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
                    ValidateRequiredText(dto.DisplayNameKey, "DisplayNameKey", dto.Id, mod);
                    ValidateRequiredText(dto.DescriptionKey, "DescriptionKey", dto.Id, mod);
                    ValidateRequiredText(dto.Category, "Category", dto.Id, mod);

                    ExpeditionCategory category = ParseCategory(dto.Category, dto.Id, mod);
                    int minPlayerLevel = ParseProgressionTier(ResolveProgressionTier(dto), dto.Id, mod);

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

        /// <summary>
        /// Writes the expedition JSON payload to the local mod save folder for inspection only,
        /// overwriting any existing cache on each server join.
        /// </summary>
        public static void WriteExpeditionJsonCache(string json)
        {
            Mod mod = ModContent.GetInstance<ExpeditionsReforged>();
            string filePath = GetExpeditionJsonPath(mod);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);
                File.WriteAllText(filePath, json ?? string.Empty);
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to write expedition JSON cache to '{filePath}'.", ex);
            }
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

        private static string ResolveProgressionTier(ExpeditionDefinitionDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.MinProgressionTier))
            {
                return dto.MinProgressionTier;
            }

            if (dto.MinPlayerLevel.HasValue && dto.MinPlayerLevel.Value > 0)
            {
                return dto.MinPlayerLevel.Value.ToString(CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        private static void ValidateRequiredText(string value, string fieldName, string expeditionId, Mod mod)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ThrowLoggedError(mod, $"Expedition '{expeditionId}' is missing required field '{fieldName}'.", new InvalidDataException($"Missing required field '{fieldName}'."));
            }
        }

        private static string GetExpeditionJsonPath(Mod mod)
        {
            string saveFolder = Path.Combine(Main.SavePath, "ModLoader", mod.Name);
            Directory.CreateDirectory(saveFolder);
            return Path.Combine(saveFolder, ExpeditionsFileName);
        }

        private static bool TryEnsureDefaultJsonExists(Mod mod, string filePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? string.Empty);

                byte[] embeddedBytes = mod.GetFileBytes(EmbeddedExpeditionsAssetPath);
                if (embeddedBytes is null || embeddedBytes.Length == 0)
                {
                    mod.Logger.Error($"Embedded expedition JSON asset '{EmbeddedExpeditionsAssetPath}' was missing or empty.");
                    return false;
                }

                File.WriteAllBytes(filePath, embeddedBytes);
                mod.Logger.Info($"Copied default expedition JSON to '{filePath}'.");
                return true;
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Failed to copy embedded expedition JSON to '{filePath}'.", ex);
                return false;
            }
        }

        private static IReadOnlyList<ExpeditionDefinitionDto> DeserializeExpeditionDtos(string json, string sourceLabel, Mod mod)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                // Invalid/empty payloads should not crash loading; log and return empty list instead.
                mod.Logger.Error($"Expedition JSON payload from '{sourceLabel}' was empty.");
                return Array.Empty<ExpeditionDefinitionDto>();
            }

            try
            {
                List<ExpeditionDefinitionDto>? deserialized = JsonSerializer.Deserialize<List<ExpeditionDefinitionDto>>(json, SerializerOptions);
                if (deserialized is null)
                {
                    // Null deserialization indicates invalid JSON or schema mismatch.
                    mod.Logger.Error($"Expedition JSON payload from '{sourceLabel}' deserialized to null.");
                    return Array.Empty<ExpeditionDefinitionDto>();
                }

                return deserialized;
            }
            catch (JsonException ex)
            {
                mod.Logger.Error($"Invalid expedition JSON in '{sourceLabel}'.", ex);
            }
            catch (Exception ex)
            {
                mod.Logger.Error($"Unexpected error while parsing expedition JSON in '{sourceLabel}'.", ex);
            }

            return Array.Empty<ExpeditionDefinitionDto>();
        }

        [DoesNotReturn]
        private static void ThrowLoggedError(Mod mod, string message, Exception exception)
        {
            mod.Logger.Error(message, exception);
            throw exception;
        }
    }
}

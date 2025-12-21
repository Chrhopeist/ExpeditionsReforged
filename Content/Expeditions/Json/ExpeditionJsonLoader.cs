using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using ExpeditionsReforged.Systems.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Content.Expeditions.Json
{
    /// <summary>
    /// Loads expedition definitions from JSON files embedded in the mod content.
    /// </summary>
    public static class ExpeditionJsonLoader
    {
        private const string ExpeditionsFileName = "expeditions.json";
        // Keep the embedded expedition JSON alongside the loader so it is bundled into mod content.
        private const string EmbeddedExpeditionsAssetPath = "expeditions.json";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// Loads expedition DTOs from mod-embedded JSON files.
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

            mod.Logger.Info("[Expeditions] ExpeditionJsonLoader.LoadExpeditionDtos invoked.");
            foreach (string file in mod.GetFileNames())
            {
                mod.Logger.Info($"[Expeditions] Mod file: {file}");
            }

            List<string> expeditionJsonFiles = mod.GetFileNames()
                .Where(IsExpeditionJsonFile)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (expeditionJsonFiles.Count == 0)
            {
                mod.Logger.Warn("[Expeditions] No expedition JSON files found under 'Content/Expeditions/Json/'.");
                return Array.Empty<ExpeditionDefinitionDto>();
            }

            mod.Logger.Info($"[Expeditions] Expedition JSON files discovered: {string.Join(", ", expeditionJsonFiles)}");

            var dtos = new List<ExpeditionDefinitionDto>();
            foreach (string expeditionFile in expeditionJsonFiles)
            {
                mod.Logger.Info($"[Expeditions] Loading expedition JSON from '{expeditionFile}'.");

                try
                {
                    // Use compiled mod content; filesystem paths are not authoritative in tModLoader.
                    using Stream stream = mod.GetFileStream(expeditionFile);
                    if (stream is null)
                    {
                        mod.Logger.Error($"[Expeditions] Failed to open stream for expedition JSON '{expeditionFile}'.");
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        mod.Logger.Error($"[Expeditions] Expedition JSON file '{expeditionFile}' was empty.");
                        continue;
                    }

                    IReadOnlyList<ExpeditionDefinitionDto> fileDtos = DeserializeExpeditionDtos(json, expeditionFile, mod);
                    mod.Logger.Info($"[Expeditions] Deserialized {fileDtos.Count} expedition DTO(s) from '{expeditionFile}'.");
                    dtos.AddRange(fileDtos);
                }
                catch (Exception ex)
                {
                    mod.Logger.Error($"[Expeditions] Failed to read expedition JSON from '{expeditionFile}'.", ex);
                }
            }

            mod.Logger.Info($"[Expeditions] Total expedition DTOs deserialized: {dtos.Count}.");
            return dtos.AsReadOnly();
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
        public static IReadOnlyList<ExpeditionDefinition> BuildDefinitions(
            IReadOnlyList<ExpeditionDefinitionDto> dtos,
            ExpeditionLoadDiagnostics diagnostics = null)
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
                ExpeditionDefinitionDto dto = dtos[index];
                string expeditionId = dto?.Id;
                if (string.IsNullOrWhiteSpace(expeditionId))
                {
                    expeditionId = $"<index:{index}>";
                }

                if (dto is null)
                {
                    string reason = $"Expedition entry at index {index} is null.";
                    mod.Logger.Warn(reason);
                    diagnostics?.RecordFailure(expeditionId, reason);
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(dto.Id))
                    {
                        throw new InvalidDataException($"Expedition entry at index {index} has an empty Id.");
                    }

                    if (!seenIds.Add(dto.Id))
                    {
                        throw new InvalidDataException($"Duplicate expedition Id '{dto.Id}' detected.");
                    }

                    ValidateRequiredText(dto.DisplayNameKey, "DisplayNameKey", dto.Id);
                    ValidateRequiredText(dto.DescriptionKey, "DescriptionKey", dto.Id);
                    ValidateRequiredText(dto.Category, "Category", dto.Id);

                    ExpeditionCategory category = ParseCategory(dto.Category, dto.Id);
                    int minPlayerLevel = ParseProgressionTier(ResolveProgressionTier(dto), dto.Id);

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
                        questGiverNpcId: ResolveQuestGiverNpcId(dto),
                        prerequisites: prerequisites,
                        deliverables: deliverables,
                        rewards: rewards,
                        dailyRewards: dailyRewards));
                }
                catch (Exception ex)
                {
                    string message = $"Failed to build expedition definition '{expeditionId}': {ex.Message}";
                    mod.Logger.Warn(message);
                    diagnostics?.RecordFailure(expeditionId, ex.Message);
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

        private static ExpeditionCategory ParseCategory(string categoryValue, string expeditionId)
        {
            if (!Enum.TryParse(categoryValue, true, out ExpeditionCategory category) ||
                !Enum.IsDefined(typeof(ExpeditionCategory), category) ||
                category == ExpeditionCategory.Unknown)
            {
                throw new InvalidDataException($"Expedition '{expeditionId}' uses invalid category '{categoryValue}'.");
            }

            return category;
        }

        /// <summary>
        /// Resolves the quest giver NPCID, preferring the new field while preserving legacy npcHeadId imports.
        /// </summary>
        private static int ResolveQuestGiverNpcId(ExpeditionDefinitionDto dto)
        {
            if (dto.QuestGiverNpcId != default)
            {
                return dto.QuestGiverNpcId;
            }

            if (dto.LegacyNpcHeadId.HasValue)
            {
                return dto.LegacyNpcHeadId.Value;
            }

            return default;
        }

        private static int ParseProgressionTier(string tierValue, string expeditionId)
        {
            // Progression tiers are represented as positive integer strings until a dedicated progression system is implemented.
            string trimmed = tierValue?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmed))
            {
                return 1;
            }

            if (!int.TryParse(trimmed, out int minPlayerLevel) || minPlayerLevel < 1)
            {
                throw new InvalidDataException($"Expedition '{expeditionId}' uses invalid progression tier '{tierValue}'.");
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

        private static void ValidateRequiredText(string value, string fieldName, string expeditionId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException($"Expedition '{expeditionId}' is missing required field '{fieldName}'.");
            }
        }

        private static bool IsExpeditionJsonFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            return fileName.StartsWith("Content/Expeditions/Json/", StringComparison.OrdinalIgnoreCase)
                && fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
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

    }
}

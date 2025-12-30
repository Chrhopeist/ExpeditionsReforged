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
    /// Loads expedition definitions from JSON files on disk, seeding the save folder from embedded assets when needed.
    /// </summary>
    public static class ExpeditionJsonLoader
    {
        private const string ExpeditionCacheFileName = "expedition_sync_cache.json";
        private const string ExpeditionJsonFolderName = "Expeditions";

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
            string folderPath = GetExpeditionJsonFolderPath(mod);

            List<string> expeditionJsonFiles = DiscoverExpeditionJsonFiles(mod, folderPath);
            if (expeditionJsonFiles.Count == 0)
            {
                mod.Logger.Warn("[Expeditions] No expedition JSON files found. Falling back to embedded defaults bundled with the mod.");
                return LoadEmbeddedExpeditionDtos(mod);
            }

            mod.Logger.Info($"[Expeditions] Expedition JSON files discovered: {string.Join(", ", expeditionJsonFiles)}");

            var dtos = new List<ExpeditionDefinitionDto>();
            foreach (string expeditionFile in expeditionJsonFiles)
            {
                mod.Logger.Info($"[Expeditions] Loading expedition JSON from '{expeditionFile}'.");

                try
                {
                    string json = File.ReadAllText(expeditionFile);
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

                    int questGiverNpcId = NormalizeQuestGiverNpcId(dto, expeditionId, mod);

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
                        questGiverNpcId: questGiverNpcId,
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
            string filePath = GetExpeditionJsonCachePath(mod);

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

        private static List<string> DiscoverExpeditionJsonFiles(Mod mod, string folderPath)
        {
            var discovered = new List<string>();

            try
            {
                Directory.CreateDirectory(folderPath);
                EnsureDefaultJsonFiles(mod, folderPath);

                discovered = Directory.EnumerateFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly)
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (Exception ex)
            {
                mod.Logger.Warn($"[Expeditions] Failed to enumerate expedition JSON files in '{folderPath}'.", ex);
            }

            return discovered;
        }

        private static IReadOnlyList<ExpeditionDefinitionDto> LoadEmbeddedExpeditionDtos(Mod mod)
        {
            List<string> embeddedJsonFiles = mod.GetFileNames()
                .Where(IsExpeditionJsonFile)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (embeddedJsonFiles.Count == 0)
            {
                mod.Logger.Warn("[Expeditions] No embedded expedition JSON files were bundled with the mod.");
                return Array.Empty<ExpeditionDefinitionDto>();
            }

            var dtos = new List<ExpeditionDefinitionDto>();
            foreach (string expeditionFile in embeddedJsonFiles)
            {
                mod.Logger.Info($"[Expeditions] Loading embedded expedition JSON from '{expeditionFile}'.");

                try
                {
                    using Stream stream = mod.GetFileStream(expeditionFile);
                    if (stream is null)
                    {
                        mod.Logger.Error($"[Expeditions] Failed to open stream for embedded expedition JSON '{expeditionFile}'.");
                        continue;
                    }

                    using var reader = new StreamReader(stream);
                    string json = reader.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        mod.Logger.Error($"[Expeditions] Embedded expedition JSON file '{expeditionFile}' was empty.");
                        continue;
                    }

                    IReadOnlyList<ExpeditionDefinitionDto> fileDtos = DeserializeExpeditionDtos(json, expeditionFile, mod);
                    mod.Logger.Info($"[Expeditions] Deserialized {fileDtos.Count} expedition DTO(s) from embedded '{expeditionFile}'.");
                    dtos.AddRange(fileDtos);
                }
                catch (Exception ex)
                {
                    mod.Logger.Error($"[Expeditions] Failed to read embedded expedition JSON from '{expeditionFile}'.", ex);
                }
            }

            mod.Logger.Info($"[Expeditions] Total embedded expedition DTOs deserialized: {dtos.Count}.");
            return dtos.AsReadOnly();
        }

        private static string GetExpeditionJsonFolderPath(Mod mod)
        {
            string saveFolder = Path.Combine(Main.SavePath, "ModLoader", mod.Name, ExpeditionJsonFolderName);
            Directory.CreateDirectory(saveFolder);
            return saveFolder;
        }

        private static string GetExpeditionJsonCachePath(Mod mod)
        {
            string folderPath = GetExpeditionJsonFolderPath(mod);
            return Path.Combine(folderPath, ExpeditionCacheFileName);
        }

        private static void EnsureDefaultJsonFiles(Mod mod, string folderPath)
        {
            List<string> embeddedJsonFiles = mod.GetFileNames()
                .Where(IsExpeditionJsonFile)
                .ToList();

            if (embeddedJsonFiles.Count == 0)
            {
                mod.Logger.Warn("[Expeditions] No embedded expedition JSON assets were found to seed the save folder.");
                return;
            }

            foreach (string embeddedPath in embeddedJsonFiles)
            {
                string fileName = Path.GetFileName(embeddedPath);
                string destinationPath = Path.Combine(folderPath, fileName);

                if (File.Exists(destinationPath))
                {
                    continue;
                }

                try
                {
                    byte[] embeddedBytes = mod.GetFileBytes(embeddedPath);
                    if (embeddedBytes is null || embeddedBytes.Length == 0)
                    {
                        mod.Logger.Warn($"[Expeditions] Embedded expedition JSON asset '{embeddedPath}' was missing or empty.");
                        continue;
                    }

                    File.WriteAllBytes(destinationPath, embeddedBytes);
                    mod.Logger.Info($"[Expeditions] Seeded default expedition JSON '{destinationPath}' from embedded asset '{embeddedPath}'.");
                }
                catch (Exception ex)
                {
                    mod.Logger.Warn($"[Expeditions] Failed to copy embedded expedition JSON '{embeddedPath}' to '{destinationPath}'.", ex);
                }
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
        /// Resolves and normalizes the quest giver NPCID, preferring the new field while preserving legacy npcHeadId imports.
        /// </summary>
        private static int NormalizeQuestGiverNpcId(
            ExpeditionDefinitionDto dto,
            string expeditionId,
            Mod mod)
        {
            int questGiverNpcId = ResolveQuestGiverNpcId(dto);
            if (questGiverNpcId < 0)
            {
                int fallbackNpcId = NPCID.Guide;
                string message = $"Expedition '{expeditionId}' has invalid quest giver NPCID {questGiverNpcId}; defaulting to NPCID.Guide ({fallbackNpcId}).";
                mod.Logger.Warn(message);
                return fallbackNpcId;
            }

            return questGiverNpcId;
        }

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

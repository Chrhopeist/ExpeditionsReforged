using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Content.Expeditions.Json;
using ExpeditionsReforged.Systems.Diagnostics;
using Terraria;
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
        private IReadOnlyList<ExpeditionDefinitionDto> _definitionDtos = Array.Empty<ExpeditionDefinitionDto>();
        private ExpeditionLoadDiagnostics _loadDiagnostics;

        public IReadOnlyCollection<ExpeditionDefinition> Definitions => _definitionCollection;

        public override void Load()
        {
            ClearDefinitions();
            _loadDiagnostics = null;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Multiplayer clients receive expedition definitions from server sync to prevent mismatches.
                Mod.Logger.Info("Expedition registry initialized in client mode; awaiting server sync.");
                _definitionDtos = Array.Empty<ExpeditionDefinitionDto>();
                return;
            }

            _loadDiagnostics = new ExpeditionLoadDiagnostics();
            IReadOnlyList<ExpeditionDefinitionDto> dtos = ExpeditionJsonLoader.LoadExpeditionDtos();
            _definitionDtos = dtos;
            IReadOnlyList<ExpeditionDefinition> definitions = ExpeditionJsonLoader.BuildDefinitions(dtos, _loadDiagnostics);
            FinalizeDefinitions(definitions);
            _loadDiagnostics.WriteLog(Mod);
        }

        public override void Unload()
        {
            ClearDefinitions();
            _definitionDtos = Array.Empty<ExpeditionDefinitionDto>();
            _loadDiagnostics = null;
        }

        public IReadOnlyCollection<ExpeditionDefinition> GetAll() => _definitionCollection;

        /// <summary>
        /// Serializes the currently loaded expedition DTOs for multiplayer sync.
        /// </summary>
        public string BuildDefinitionSyncJson()
        {
            return ExpeditionJsonLoader.SerializeExpeditionDtos(_definitionDtos);
        }

        /// <summary>
        /// Applies a definition sync payload on multiplayer clients, rebuilding the registry from the JSON data.
        /// </summary>
        public void ApplyDefinitionSync(string json)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Cache the server-provided JSON for inspection only; the server remains authoritative.
            // This file is overwritten on each server join when the sync payload arrives.
            ExpeditionJsonLoader.WriteExpeditionJsonCache(json);

            try
            {
                IReadOnlyList<ExpeditionDefinitionDto> dtos = ExpeditionJsonLoader.DeserializeExpeditionDtos(json);
                _definitionDtos = dtos;
                IReadOnlyList<ExpeditionDefinition> definitions = ExpeditionJsonLoader.BuildDefinitions(dtos);
                FinalizeDefinitions(definitions);
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("Failed to apply expedition definition sync payload.", ex);
                _definitionDtos = Array.Empty<ExpeditionDefinitionDto>();
                ClearDefinitions();
            }
        }

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

        private void ClearDefinitions()
        {
            _definitions = new ReadOnlyDictionary<string, ExpeditionDefinition>(new Dictionary<string, ExpeditionDefinition>(StringComparer.Ordinal));
            _definitionCollection = Array.Empty<ExpeditionDefinition>();
        }

        private bool TryValidateDefinition(ExpeditionDefinition definition, IDictionary<string, ExpeditionDefinition> existing, out ExpeditionDefinition validated)
        {
            validated = null;

            if (definition is null)
            {
                return FailValidation("<null>", "Skipping null expedition definition.");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    return FailValidation("<unknown>", "Expedition definitions must supply a non-empty ID. Definition skipped.");
                }

                if (existing.ContainsKey(definition.Id))
                {
                    return FailValidation(definition.Id, $"Duplicate expedition id '{definition.Id}' detected during registration. The duplicate was ignored.");
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' has no display name and was skipped.");
                }

                if (!Enum.IsDefined(typeof(ExpeditionCategory), definition.Category) || definition.Category == ExpeditionCategory.Unknown)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' uses an undefined category '{definition.Category}'. The definition was skipped.");
                }

                if (definition.QuestGiverNpcId >= 0 && (definition.QuestGiverNpcId >= TextureAssets.NpcHead.Length))
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' references invalid quest giver NPCID {definition.QuestGiverNpcId}. The definition was skipped.");
                }

                if (definition.DurationTicks <= 0)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' has non-positive duration and was skipped.");
                }

                if (definition.Difficulty <= 0)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' has non-positive difficulty and was skipped.");
                }

                if (definition.MinPlayerLevel < 0)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' has a negative minimum player level and was skipped.");
                }

                if (definition.Rarity < 0)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' has a negative rarity and was skipped.");
                }

                if (!ValidateCollections(definition))
                    return false;

                validated = definition.Clone();
                _loadDiagnostics?.RecordSuccess(validated.Id);
                return true;
            }
            catch (Exception ex)
            {
                string expeditionId = definition?.Id ?? "<unknown>";
                return FailValidation(expeditionId, $"Failed to register expedition '{expeditionId}': {ex.Message}");
            }
        }

        private bool ValidateCollections(ExpeditionDefinition definition)
        {
            foreach (var prerequisite in definition.Prerequisites)
            {
                if (prerequisite is null)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' contains a null prerequisite and was skipped.");
                }
            }

            foreach (var deliverable in definition.Deliverables)
            {
                if (deliverable is null)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' contains a null deliverable and was skipped.");
                }
            }

            foreach (var reward in definition.Rewards)
            {
                if (reward is null)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' contains a null reward and was skipped.");
                }
            }

            foreach (var reward in definition.DailyRewards)
            {
                if (reward is null)
                {
                    return FailValidation(definition.Id, $"Expedition '{definition.Id}' contains a null daily reward and was skipped.");
                }
            }

            return true;
        }

        private bool FailValidation(string expeditionId, string message)
        {
            Mod.Logger.Warn(message);
            _loadDiagnostics?.RecordFailure(expeditionId, message);
            return false;
        }
    }
}

using System;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Server-authoritative service for expedition lifecycle operations.
    /// All state mutations go through this service to ensure multiplayer safety.
    /// </summary>
    public static class ExpeditionService
    {
        /// <summary>
        /// Determines if a player can accept the provided expedition definition.
        /// This is the single authoritative validation rule set for expedition acceptance.
        /// </summary>
        /// <param name="player">The player attempting to accept an expedition.</param>
        /// <param name="definition">The expedition definition to validate.</param>
        /// <param name="failReasonKey">Localization key describing why acceptance failed, if applicable.</param>
        /// <returns>True if the expedition can be accepted; otherwise false.</returns>
        public static bool CanAcceptExpedition(Player player, ExpeditionDefinition definition, out string? failReasonKey)
        {
            failReasonKey = null;

            if (player == null || !player.active)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.InvalidPlayer";
                return false;
            }

            if (definition == null)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.ExpeditionNotFound";
                return false;
            }

            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.InvalidExpeditionId";
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetExpedition(definition.Id, out ExpeditionDefinition registryDefinition))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.ExpeditionNotFound";
                return false;
            }

            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer == null)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.PlayerDataMissing";
                return false;
            }

            // Do not accept an expedition that is already active.
            if (expeditionsPlayer.IsExpeditionActive(registryDefinition.Id))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.AlreadyActive";
                return false;
            }

            // Non-repeatable expeditions can only be accepted once.
            if (!registryDefinition.IsRepeatable && expeditionsPlayer.IsExpeditionCompleted(registryDefinition.Id))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.AlreadyCompleted";
                return false;
            }

            if (!MeetsProgressionRequirement(player, registryDefinition))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.ProgressionTooLow";
                return false;
            }

            if (!MeetsPrerequisites(player, registryDefinition))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.PrerequisitesNotMet";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to start an expedition for the given player. This is the single authority point
        /// for starting expeditions; UI and network handlers should route through this method.
        /// </summary>
        /// <param name="player">The player starting the expedition.</param>
        /// <param name="expeditionId">The ID of the expedition to start.</param>
        /// <param name="failReasonKey">Localization key describing why the operation failed, if applicable.</param>
        /// <returns>True if the expedition was started successfully; otherwise false.</returns>
        public static bool TryStartExpedition(Player player, string expeditionId, out string? failReasonKey)
        {
            failReasonKey = null;

            // Guard: Clients cannot mutate gameplay state directly
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.ClientCannotStart";
                return false;
            }

            // Validate expedition ID
            if (string.IsNullOrWhiteSpace(expeditionId))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.InvalidExpeditionId";
                return false;
            }

            // Resolve the expedition definition before validating acceptance.
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            registry.TryGetExpedition(expeditionId, out ExpeditionDefinition definition);
            if (!CanAcceptExpedition(player, definition, out failReasonKey))
            {
                return false;
            }

            // Server-authoritative state mutation: start the expedition
            player.GetModPlayer<ExpeditionsPlayer>().StartExpedition(definition.Id, Main.GameUpdateCount);

            return true;
        }

        /// <summary>
        /// Validates whether the player meets the prerequisites for the given expedition.
        /// This does not mutate state and can be called from UI or validation flows.
        /// </summary>
        /// <param name="player">The player to check prerequisites for.</param>
        /// <param name="definition">The expedition definition to validate against.</param>
        /// <returns>True if all prerequisites are satisfied; otherwise false.</returns>
        public static bool MeetsPrerequisites(Player player, ExpeditionDefinition definition)
        {
            if (player == null || definition == null)
            {
                return false;
            }

            // Prerequisite validation will leverage expedition condition tracking once those
            // condition entries are surfaced to the UI and server validation flow.

            return true;
        }

        /// <summary>
        /// Determines whether the player meets the progression tier required by the expedition.
        /// Uses Terraria world progression flags as the current stand-in for expedition tiers.
        /// </summary>
        public static bool MeetsProgressionRequirement(Player player, ExpeditionDefinition definition)
        {
            if (player == null || definition == null)
            {
                return false;
            }

            int requiredTier = Math.Max(1, definition.MinPlayerLevel);
            int currentTier = GetWorldProgressionTier();
            return currentTier >= requiredTier;
        }

        private static int GetWorldProgressionTier()
        {
            int tier = 1;

            if (NPC.downedBoss1)
            {
                tier = Math.Max(tier, 2);
            }

            if (NPC.downedBoss2 || NPC.downedBoss3)
            {
                tier = Math.Max(tier, 3);
            }

            if (Main.hardMode)
            {
                tier = Math.Max(tier, 4);
            }

            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
            {
                tier = Math.Max(tier, 5);
            }

            if (NPC.downedPlantBoss)
            {
                tier = Math.Max(tier, 6);
            }

            if (NPC.downedGolemBoss)
            {
                tier = Math.Max(tier, 7);
            }

            if (NPC.downedMoonlord)
            {
                tier = Math.Max(tier, 8);
            }

            return tier;
        }

        /// <summary>
        /// Determines whether the provided NPC can offer expeditions to the specified player.
        /// This is a client-safe query that does not mutate any gameplay state.
        /// </summary>
        /// <param name="npcType">The NPC type to evaluate.</param>
        /// <param name="player">The player interacting with the NPC.</param>
        /// <returns>True if at least one expedition is available from this NPC; otherwise false.</returns>
        public static bool IsExpeditionGiver(int npcType, Player player)
        {
            if (player == null || npcType < 0)
            {
                return false;
            }

            if (player.GetModPlayer<ExpeditionsPlayer>() == null)
            {
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();

            foreach (ExpeditionDefinition definition in registry.Definitions)
            {
                if (definition.QuestGiverNpcId != npcType)
                {
                    continue;
                }

                // Match the expedition acceptance rules so the button only appears when something is available.
                if (CanAcceptExpedition(player, definition, out _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

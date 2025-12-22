using System;
using System.Collections.Generic;
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

            // Validate player
            if (player == null || !player.active)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.InvalidPlayer";
                return false;
            }

            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            if (expeditionsPlayer == null)
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.PlayerDataMissing";
                return false;
            }

            // Validate expedition definition exists
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            if (!registry.TryGetExpedition(expeditionId, out ExpeditionDefinition definition))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.ExpeditionNotFound";
                return false;
            }

            // Check if expedition is already active
            if (expeditionsPlayer.IsExpeditionActive(expeditionId))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.AlreadyActive";
                return false;
            }

            // Check repeatability: non-repeatable expeditions can only be started once
            if (!definition.IsRepeatable && expeditionsPlayer.IsExpeditionCompleted(expeditionId))
            {
                failReasonKey = "Mods.ExpeditionsReforged.Errors.AlreadyCompleted";
                return false;
            }

            // TODO: Enforce MinPlayerLevel once a player progression system exists.
            // For now, MinPlayerLevel is defined on the expedition but not validated here.
            // When a level system is implemented, add:
            // if (player.GetPlayerLevel() < definition.MinPlayerLevel)
            // {
            //     failReasonKey = "Mods.ExpeditionsReforged.Errors.LevelTooLow";
            //     return false;
            // }

            // Server-authoritative state mutation: start the expedition
            expeditionsPlayer.StartExpedition(expeditionId, Main.GameUpdateCount);

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

            // TODO: Implement prerequisite validation once condition tracking is wired.
            // For now, all prerequisites are assumed satisfied to allow testing.
            // When conditions are implemented, iterate definition.Prerequisites and check
            // against ExpeditionsPlayer's condition state or world state as appropriate.

            return true;
        }

        /// <summary>
        /// Determines whether the supplied NPC type can currently offer expeditions to the player.
        /// This is a pure query with no state mutations and is safe to use from UI hooks.
        /// </summary>
        /// <param name="npcType">The NPC type to check.</param>
        /// <param name="player">The player interacting with the NPC.</param>
        /// <returns>True if at least one eligible expedition is available; otherwise false.</returns>
        public static bool IsExpeditionGiver(int npcType, Player player)
        {
            if (player == null || !player.active)
            {
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            IEnumerable<ExpeditionDefinition> definitions = registry.Definitions;

            foreach (ExpeditionDefinition definition in definitions)
            {
                if (definition.QuestGiverNpcId != npcType)
                {
                    continue;
                }

                // Hide already-active expeditions and completed non-repeatable entries.
                if (expeditionsPlayer.IsExpeditionActive(definition.Id))
                {
                    continue;
                }

                if (!definition.IsRepeatable && expeditionsPlayer.IsExpeditionCompleted(definition.Id))
                {
                    continue;
                }

                if (!MeetsPrerequisites(player, definition))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}

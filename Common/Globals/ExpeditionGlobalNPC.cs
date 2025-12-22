using System.Collections.Generic;
using ExpeditionsReforged.Content.Expeditions;
using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    public class ExpeditionGlobalNPC : GlobalNPC
    {
        public override void ModifyChatButtons(NPC npc, List<string> buttons)
        {
            if (!ShouldShowExpeditionButton(npc, Main.LocalPlayer))
            {
                return;
            }

            // Use the second chat button slot for the expedition UI, mirroring the legacy behavior.
            if (buttons.Count == 1)
            {
                buttons.Add("Expedition");
                return;
            }

            if (buttons.Count > 1)
            {
                buttons[1] = "Expedition";
            }
        }

        public override void OnChatButtonClicked(NPC npc, int button, ref bool shop)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            if (button != 1)
            {
                return;
            }

            if (!ShouldShowExpeditionButton(npc, Main.LocalPlayer))
            {
                return;
            }

            // NPC chat buttons are client-side UI actions; opening the UI stays client-only.
            ModContent.GetInstance<ExpeditionsSystem>().OpenNpcExpeditionUI(npc.type);
        }

        public override void OnKill(NPC npc)
        {
            if (npc.lastInteraction < 0 || npc.lastInteraction >= Main.maxPlayers)
            {
                return;
            }

            Player player = Main.player[npc.lastInteraction];
            if (player?.active == true && !player.dead)
            {
                player.GetModPlayer<ExpeditionsPlayer>().ReportKill(npc);
            }
        }

        private static bool ShouldShowExpeditionButton(NPC npc, Player player)
        {
            if (npc == null || player == null || !player.active)
            {
                return false;
            }

            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            IEnumerable<ExpeditionDefinition> definitions = registry.Definitions;

            foreach (ExpeditionDefinition definition in definitions)
            {
                if (definition.QuestGiverNpcId != npc.type)
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

                if (!ExpeditionService.MeetsPrerequisites(player, definition))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}

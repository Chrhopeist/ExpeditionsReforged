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
        public override void SetChatButtons(NPC npc, ref string button, ref string button2)
        {
            if (!ShouldShowExpeditionButton(npc, Main.LocalPlayer))
            {
                return;
            }

            // Follow the ExampleMod pattern by using the second chat button for the expedition UI.
            button2 = "Expedition";
        }

        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            if (firstButton || Main.netMode == NetmodeID.Server)
            {
                return;
            }

            if (!ShouldShowExpeditionButton(npc, Main.LocalPlayer))
            {
                return;
            }

            // NPC chat buttons are client-side UI actions; opening the UI is safe here.
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

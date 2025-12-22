using System.Collections.Generic;
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
            if (!ExpeditionService.IsExpeditionGiver(npc.type, Main.LocalPlayer))
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

            if (!ExpeditionService.IsExpeditionGiver(npc.type, Main.LocalPlayer))
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

    }
}

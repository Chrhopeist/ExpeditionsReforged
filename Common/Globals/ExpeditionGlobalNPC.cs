using ExpeditionsReforged.Players;
using Terraria;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    public class ExpeditionGlobalNPC : GlobalNPC
    {
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

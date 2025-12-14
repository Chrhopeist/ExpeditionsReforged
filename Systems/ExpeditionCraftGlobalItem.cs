using ExpeditionsReforged.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    public class ExpeditionCraftGlobalItem : GlobalItem
    {
        public override void OnCraft(Item item, Recipe recipe)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (player is null || !player.active)
            {
                return;
            }

            player.GetModPlayer<ExpeditionsPlayer>().ReportCraft(item);
        }
    }
}

using ExpeditionsReforged.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    public class ExpeditionGlobalItem : GlobalItem
    {
        public override bool OnPickup(Item item, Player player)
        {
            player.GetModPlayer<ExpeditionsPlayer>().ReportItemPickup(item);
            return base.OnPickup(item, player);
        }

        public override void OnCreated(Item item, ItemCreationContext context)
        {
            // ModPlayer.OnCraft was removed in tModLoader 1.4.4, so crafting needs to be detected through
            // the item creation pipeline instead. GlobalItem.OnCreated runs for crafted items and exposes
            // the RecipeItemCreationContext we need on this version.
            if (context is not RecipeItemCreationContext recipeContext)
            {
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Only the server/host should record crafting progress to avoid double-reporting in multiplayer.
                return;
            }

            Player craftingPlayer = recipeContext.player;
            craftingPlayer?.GetModPlayer<ExpeditionsPlayer>().ReportCraft(item);
        }
    }
}

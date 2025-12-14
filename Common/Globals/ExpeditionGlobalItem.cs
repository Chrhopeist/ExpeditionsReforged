using ExpeditionsReforged.Players;
using Terraria;
using Terraria.DataStructures;
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
            if (context is not RecipeItemCreationContext)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            player?.GetModPlayer<ExpeditionsPlayer>().ReportCraft(item);
        }
    }
}

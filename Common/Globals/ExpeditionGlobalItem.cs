using ExpeditionsReforged.Players;
using Terraria;
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
    }
}

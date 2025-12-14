using ExpeditionsReforged.Players;
using Terraria;
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

        /*
         * NOTE:
         * Craft detection is intentionally NOT handled here.
         *
         * In tModLoader 1.4.4:
         * - GlobalItem does not expose a valid OnCreate/OnCreated hook
         * - RecipeItemCreationContext does not provide a player reference
         *
         * Craft progress will be wired later using an authoritative,
         * multiplayer-safe system (likely via expedition turn-ins or board actions).
         */
    }
}

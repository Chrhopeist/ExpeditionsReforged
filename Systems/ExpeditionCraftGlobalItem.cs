using ExpeditionsReforged.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Systems
{
    public class ExpeditionCraftGlobalItem : GlobalItem
    {
        public override void OnCreate(Item item, ItemCreationContext context)
        {
            if (context is RecipeItemCreationContext recipeContext)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }

                recipeContext.player.GetModPlayer<ExpeditionsPlayer>().ReportCraft(item);
            }
        }
    }
}

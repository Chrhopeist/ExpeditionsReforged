using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Expeditions.Items
{
    public class StockBox2 : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Relic Box");
            Tooltip.SetDefault("Right click to open\n"
              + "'Its contents, an enigma...'");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 24;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.LightRed;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            int rare = ItemRewardPool.GetRewardRare(player);
            if (rare < 3) rare = 3;
            IEntitySource source = player.GetSource_OpenItem(Type);
            try
            {
                foreach (ItemRewardData i in ItemRewardPool.GenerateFullRewards(rare))
                {
                    player.QuickSpawnItem(source, i.itemID, i.stack);
                }
            }
            catch (System.Exception)
            {
                player.QuickSpawnItem(source, ItemID.GoldenCrate);
            }
        }
    }
}

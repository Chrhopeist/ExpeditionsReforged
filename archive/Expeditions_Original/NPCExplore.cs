using System;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Expeditions
{
    public class NPCExplore : GlobalNPC
    {
        #region Shop
        public override void ModifyShop(NPCShop shop)
        {
            if (shop.NpcType == NPCID.Merchant) MerchantShop(shop);
            if (shop.NpcType == NPCID.SkeletonMerchant) SkeletonMerchantShop(shop);
        }

        public void MerchantShop(NPCShop shop)
        {
            shop.Add(API.ItemIDExpeditionBook);
        }
        public void SkeletonMerchantShop(NPCShop shop)
        {
            if (Main.moonPhase % 2 == 0) //Alternate between selling the box and board
            { API.AddShopItemVoucher(shop, API.ItemIDRustedBox, 1); }
            else
            { shop.Add(API.ItemIDExpeditionBoard); }
        }

        internal static void AddVoucherPricedItem(NPCShop shop, int itemID, int price)
        {
            price = Math.Min(999, Math.Max(0, price));

            shop.Add(itemID, price: price, specialCurrency: Expeditions.currencyVoucherID);
        }

        #endregion

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (player.whoAmI != Main.myPlayer) return;
            foreach (ModExpedition me in Expeditions.GetExpeditionsList())
            {
                if (npc.life <= 0 || !npc.active)
                { expKillNPC(me, npc); }
                expCombatWithNPC(me, npc);
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Main.netMode == NetmodeID.Server) return;
            if (projectile.owner != Main.myPlayer) return;
            foreach (ModExpedition me in Expeditions.GetExpeditionsList())
            {
                if (npc.life <= 0 || !npc.active)
                { expKillNPC(me, npc); }
                expCombatWithNPC(me, npc);
            }
        }
        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.Server) return;
            foreach (ModExpedition me in Expeditions.GetExpeditionsList())
            {
                expAnyNPCDeath(me, npc);
            }
        }

        private void expCombatWithNPC(ModExpedition me, NPC npc)
        {
            me.OnCombatWithNPC(npc, false, Main.LocalPlayer,
                          ref me.expedition.condition1Met,
                          ref me.expedition.condition2Met,
                          ref me.expedition.condition3Met,
                          me.expedition.conditionCounted >= me.expedition.conditionCountedMax
                          );
        }
        private void expKillNPC(ModExpedition me, NPC npc)
        {
            me.OnKillNPC(npc, Main.LocalPlayer,
                          ref me.expedition.condition1Met,
                          ref me.expedition.condition2Met,
                          ref me.expedition.condition3Met,
                          me.expedition.conditionCounted >= me.expedition.conditionCountedMax
                          );
        }
        private void expAnyNPCDeath(ModExpedition me, NPC npc)
        {
            me.OnAnyNPCDeath(npc, Main.LocalPlayer,
                          ref me.expedition.condition1Met,
                          ref me.expedition.condition2Met,
                          ref me.expedition.condition3Met,
                          me.expedition.conditionCounted >= me.expedition.conditionCountedMax
                          );
        }
    }
}

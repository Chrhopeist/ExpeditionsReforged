using System;
using System.Globalization;
using ExpeditionsReforged.Content.Expeditions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ID;

namespace ExpeditionsReforged.Systems
{
    /// <summary>
    /// Server-side reward payout helper for expedition completion.
    /// Keeps gameplay logic separate from static content definitions.
    /// </summary>
    public static class ExpeditionRewardService
    {
        /// <summary>
        /// Attempts to pay completion rewards to the provided player for the given expedition definition.
        /// Intended to run only on the server; clients return false immediately.
        /// </summary>
        public static bool TryPayCompletionRewards(Player player, ExpeditionDefinition definition)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }

            if (player is null || definition is null)
            {
                return false;
            }

            ExpeditionsReforged mod = ModContent.GetInstance<ExpeditionsReforged>();
            IEntitySource rewardSource = new EntitySource_Misc("ExpeditionReward");

            foreach (RewardDefinition reward in definition.Rewards)
            {
                if (reward is null)
                {
                    continue;
                }

                if (reward.DropChance < 1f && Main.rand.NextFloat() > reward.DropChance)
                {
                    continue;
                }

                if (!TryParseItemReward(reward.Id, out int itemType))
                {
                    mod.Logger.Warn($"Expedition '{definition.Id}' has unsupported reward id '{reward.Id}'. Reward was skipped.");
                    continue;
                }

                int stack = Main.rand.Next(reward.MinStack, reward.MaxStack + 1);
                if (stack <= 0)
                {
                    continue;
                }

                player.QuickSpawnItem(rewardSource, itemType, stack);
            }

            return true;
        }

        private static bool TryParseItemReward(string rewardId, out int itemType)
        {
            itemType = 0;
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return false;
            }

            const string prefix = "item:";
            if (!rewardId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string numericPart = rewardId[prefix.Length..];
            return int.TryParse(numericPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out itemType);
        }
    }
}

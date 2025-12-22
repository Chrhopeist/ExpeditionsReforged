using ExpeditionsReforged.Players;
using ExpeditionsReforged.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ExpeditionsReforged.Common.Globals
{
    public class ExpeditionGlobalNPC : GlobalNPC
    {
        // GetChat is used only as a turn-in trigger; the chat text is intentionally untouched.
        public override void GetChat(NPC npc, ref string chat)
        {
            Player player = null;

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                player = Main.LocalPlayer;
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                // Client can only request a turn-in; the server performs the authoritative completion.
                player = Main.LocalPlayer;
            }
            else if (npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers)
            {
                player = Main.player[npc.lastInteraction];
            }

            if (player?.active != true)
            {
                return;
            }

            ExpeditionsPlayer expeditionsPlayer = player.GetModPlayer<ExpeditionsPlayer>();
            ExpeditionRegistry registry = ModContent.GetInstance<ExpeditionRegistry>();
            bool turnedIn = false;

            foreach (var progress in expeditionsPlayer.ExpeditionProgressEntries)
            {
                if (progress is null
                    || string.IsNullOrWhiteSpace(progress.ExpeditionId)
                    || !progress.IsActive
                    || !progress.IsCompleted
                    || progress.RewardsClaimed)
                {
                    continue;
                }

                if (!registry.TryGetExpedition(progress.ExpeditionId, out var definition))
                {
                    continue;
                }

                if (definition.QuestGiverNpcId != npc.type)
                {
                    continue;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // Client only requests the turn-in; the server performs validation and rewards.
                    ExpeditionsReforged.RequestNpcTurnIn(progress.ExpeditionId, npc.type);
                    return;
                }

                if (expeditionsPlayer.TryTurnInExpedition(progress.ExpeditionId, npc.type))
                {
                    turnedIn = true;
                }
            }

            if (turnedIn && Main.netMode == NetmodeID.Server)
            {
                ExpeditionsReforged.SendProgressSync(-1, player.whoAmI, expeditionsPlayer);
            }
        }

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
